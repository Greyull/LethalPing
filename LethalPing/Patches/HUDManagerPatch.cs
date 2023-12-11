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
using Steamworks;
using Steamworks.Data;

namespace LethalPing.Patches
{
    [HarmonyPatch]
    internal class HUDManagerPatch : MonoBehaviour
    {
        public static Dictionary<ulong, RectTransform> pingElements = new Dictionary<ulong, RectTransform>();

        public static bool instantiating = true;

        //public static RaycastHit[] pingHits;
        //public static Double[] pingTimes;

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix()]
        public static void NewScanElements()
        {
            if (HUDManagerPatch.instantiating)
            {
                LethalPingPlugin.mls.LogInfo("Patching scanElements...");
                GameObject refObject = HUDManager.Instance.scanElements[0].gameObject;
                Transform pingContainer = Instantiate(HUDManager.Instance.scanElements[0].transform.parent, HUDManager.Instance.scanElements[0].parent.parent);
                pingContainer.name = "pingContainer";
                //Destroying pre-existing scanElements from copied scan element container--creating an empty transform would break the animations, I'm assuming some MonoBehaviour nonsense is tying the animation to the container??? either way I don't need a ton of extra objects, that's just a waste of memory.
                for(int i=0; i<pingContainer.childCount;i++)
                {
                    GameObject.Destroy(pingContainer.GetChild(i).gameObject);
                }
                //Setting sibling index in order to make sure UI element doesn't render over top of QuickMenuManager (escape menu)
                pingContainer.SetSiblingIndex(10);
                for (int i = 0; i < GameNetworkManager.Instance.currentLobby.Value.MaxMembers; i++)
                {
                    GameObject clone = Instantiate(refObject, pingContainer);
                    pingElements[Convert.ToUInt64(i)] = clone.GetComponent<RectTransform>();
                    clone.name = $"pingObject";
                    //LethalPingPlugin.mls.LogInfo($"Creating PingObject{i}...");
                }
                HUDManagerPatch.instantiating = false;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPrefix()]
        public static void Uninstantiate()
        {
            HUDManagerPatch.instantiating = true;
        }

        /*[HarmonyPatch(typeof(StartOfRound), "OnClientConnect")]
        [HarmonyPrefix()]
        public static void AddScanElementOnServer(StartOfRound __instance, ulong clientId)
        {
            if (__instance.IsServer)
            {
                if (!HUDManagerPatch.instantiating)
                {
                    GameObject clone = Instantiate(pingElements[0].gameObject, pingElements[0].parent);
                    pingElements[clientId] = clone.GetComponent<RectTransform>();
                    clone.name = $"pingObject{clientId}";
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "OnPlayerConnectedClientRpc")]
        [HarmonyPrefix()]
        public static void AddScanElementOnClient(StartOfRound __instance, ulong clientId)
        {
            if (!__instance.IsServer)
            {
                if (!HUDManagerPatch.instantiating)
                {
                    GameObject clone = Instantiate(pingElements[0].gameObject, pingElements[0].parent);
                    pingElements[clientId] = clone.GetComponent<RectTransform>();
                    clone.name = $"pingObject{clientId}";
                }
            }
        }*/

        [HarmonyPatch(typeof(StartOfRound), "OnPlayerDC")]
        [HarmonyPrefix()]
        public static void RemoveScanElement(StartOfRound __instance, int playerObjectNumber, ulong clientId)
        {
            pingElements.Remove(clientId);
        }

        //Add hook for quitting to main menu to set instantiating to true, as pingElements and pingContainer are destroyed on exit to main menu

        [HarmonyPatch(typeof(HUDManager), "UpdateScanNodes")]
        [HarmonyPrefix()]
        private static void updatePingElements(HUDManager __instance, PlayerControllerB playerScript)
        {
            foreach (KeyValuePair<ulong, PingElement>element in PingController.Instance.pings)
            {
                if (element.Value.pingUsername != (object)null)
                {
                    if ((object)element.Value.pingLifetime != (object)null || element.Value.pingLifetime != 0)
                    {
                        if (LethalPingPlugin.GetCurTime() - element.Value.pingLifetime < 0 )
                        {
                            pingElements[element.Key].gameObject.SetActive(true);                            
                            TextMeshProUGUI[] pingElementsText = pingElements[element.Key].gameObject.GetComponentsInChildren<TextMeshProUGUI>();
                            pingElementsText[0].text = element.Value.pingUsername;
                            pingElementsText[1].text = element.Value.pingText;
                            Vector3 zero = new Vector3(0,0,0);
                            float distance;
                            float elementScale = 1;
                            if (element.Value.isAttachedToObj)
                            {
                                zero = StartOfRound.Instance.localPlayerController.gameplayCamera.WorldToScreenPoint(element.Value.attachedNode.transform.position);
                                distance = Vector3.Distance(StartOfRound.Instance.localPlayerController.transform.position, element.Value.attachedNode.transform.position);
                                if (distance > 100)
                                {
                                    pingElements[element.Key].gameObject.SetActive(false);
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
                                zero = StartOfRound.Instance.localPlayerController.gameplayCamera.WorldToScreenPoint(element.Value.pingPosition);
                                distance = Vector3.Distance(StartOfRound.Instance.localPlayerController.transform.position, element.Value.pingPosition);
                                if (distance > 100)
                                {
                                    pingElements[element.Key].gameObject.SetActive(false);
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
                            pingElements[element.Key].anchoredPosition = new Vector2(zero.x - 439.48f, zero.y - 244.8f);
                            pingElements[element.Key].localScale = new Vector3(elementScale, elementScale, elementScale);
                            pingElements[element.Key].GetComponent<Animator>().SetInteger("colorNumber", element.Value.nodeType);
                            if(zero.z < 0)
                            {
                                pingElements[element.Key].gameObject.SetActive(false);
                            }
                        } else
                        {
                            pingElements[element.Key].gameObject.SetActive(false);
                            if (element.Value.isAttachedToObj)
                            {
                                element.Value.isAttachedToObj = false;
                            }
                        }
                    }
                }
            }
        }
    }
}
