using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using JetBrains.Annotations;
using MoreCompany;
using BiggerLobby;
using GameNetcodeStuff;
using TMPro;

namespace LethalPing.Patches
{
    [HarmonyPatch]
    internal class HUDManagerPatch : MonoBehaviour
    {
        public static RectTransform[] pingElements;

        public static bool instantiating = true;

        //public static RaycastHit[] pingHits;
        //public static Double[] pingTimes;

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix()]
        public static void NewScanElements()
        {
            if (instantiating)
            {
                pingElements = new RectTransform[LethalPingPlugin.PlayerCount];
                LethalPingPlugin.mls.LogInfo("Patching scanElements...");
                GameObject refObject = HUDManager.Instance.scanElements[0].gameObject;
                Transform pingContainer = Instantiate(HUDManager.Instance.scanElements[0].transform.parent, HUDManager.Instance.scanElements[0].parent.parent);
                pingContainer.name = "pingContainer";
                for(int i=0; i<pingContainer.childCount;i++)
                {
                    GameObject.Destroy(pingContainer.GetChild(i).gameObject);
                }
                pingContainer.SetSiblingIndex(10);
                for (int i = 0; i < pingElements.Length; i++)
                {
                    GameObject clone = Instantiate(refObject, pingContainer);
                    pingElements[i] = clone.GetComponent<RectTransform>();
                    clone.name = $"pingObject{i}";
                    //LethalPingPlugin.mls.LogInfo($"Creating PingObject{i}...");
                }
                instantiating = false;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPrefix()]
        public static void unInstantiate()
        {
            instantiating = true;
        }

        //Add hook for quitting to main menu to set instantiating to true, as pingElements and pingContainer are destroyed on exit to main menu

        [HarmonyPatch(typeof(HUDManager), "UpdateScanNodes")]
        [HarmonyPrefix()]
        private static void updatePingElements(HUDManager __instance, PlayerControllerB playerScript)
        {
            for(int i=0;i<LethalPingPlugin.allPings.Length;i++)
            {
                if ((object)LethalPingPlugin.allPings[i] != (object)null)
                {
                    if ((object)LethalPingPlugin.allPings[i].pingLifetime != (object)null || LethalPingPlugin.allPings[i].pingLifetime != 0)
                    {
                        if (LethalPingPlugin.GetCurTime() - LethalPingPlugin.allPings[i].pingLifetime < 0 )
                        {
                            pingElements[i].gameObject.SetActive(true);                            
                            TextMeshProUGUI[] pingElementsText = pingElements[i].gameObject.GetComponentsInChildren<TextMeshProUGUI>();
                            pingElementsText[0].text = LethalPingPlugin.allPings[i].pingUsername;
                            pingElementsText[1].text = LethalPingPlugin.allPings[i].pingText;
                            Vector3 zero = new Vector3(0,0,0);
                            float distance;
                            float elementScale = 1;
                            if (LethalPingPlugin.allPings[i].isAttachedToObj)
                            {
                                zero = StartOfRound.Instance.localPlayerController.gameplayCamera.WorldToScreenPoint(LethalPingPlugin.allPings[i].attachedNode.transform.position);
                                distance = Vector3.Distance(StartOfRound.Instance.localPlayerController.transform.position, LethalPingPlugin.allPings[i].attachedNode.transform.position);
                                if (distance > 100)
                                {
                                    pingElements[i].gameObject.SetActive(false);
                                } else
                                {
                                    if (distance < 6) {
                                        elementScale = 1;
                                    } else
                                    {
                                        elementScale = (float)(1 / Math.Log(distance, 6));
                                    }
                                    
                                }
                            } else
                            {
                                zero = StartOfRound.Instance.localPlayerController.gameplayCamera.WorldToScreenPoint(LethalPingPlugin.allPings[i].pingPosition);
                                distance = Vector3.Distance(StartOfRound.Instance.localPlayerController.transform.position, LethalPingPlugin.allPings[i].pingPosition);
                                if (distance > 100)
                                {
                                    pingElements[i].gameObject.SetActive(false);
                                } else
                                {
                                    if (distance < 6)
                                    {
                                        elementScale = 1;
                                    } else
                                    {
                                        elementScale = (float)(1 / Math.Log(distance, 6));
                                    }
                                }
                            }
                            pingElements[i].anchoredPosition = new Vector2(zero.x - 439.48f, zero.y - 244.8f);
                            pingElements[i].localScale = new Vector3(elementScale, elementScale, elementScale);
                            pingElements[i].GetComponent<Animator>().SetInteger("colorNumber", LethalPingPlugin.allPings[i].nodeType);
                            if(zero.z < 0)
                            {
                                pingElements[i].gameObject.SetActive(false);
                            }
                        } else
                        {
                            pingElements[i].gameObject.SetActive(false);
                            if (LethalPingPlugin.allPings[i].isAttachedToObj)
                            {
                                LethalPingPlugin.allPings[i].isAttachedToObj = false;
                            }
                        }
                    }
                }
            }
        }
    }
}
