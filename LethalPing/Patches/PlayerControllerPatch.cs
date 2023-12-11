using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem.HID;

namespace LethalPing.Patches
{
    [Flags]
    public enum GameLayers
    {
        Default = 1 << 0,
        TransparentFX = 1 << 1,
        IgnoreRaycast = 1 << 2,
        Player = 1 << 3,
        Water = 1 << 4,
        UI = 1 << 5,
        Props = 1 << 6,
        HelmetVisor = 1 << 7,
        Room = 1 << 8,
        InteractableObject = 1 << 9,
        Foliage = 1 << 10,
        Colliders = 1 << 11,
        PhysicsObject = 1 << 12,
        Triggers = 1 << 13,
        MapRadar = 1 << 14,
        NavigationSurface = 1 << 15,
        RoomLight = 1 << 16,
        Anomaly = 1 << 17,
        LineOfSight = 1 << 18,
        Enemies = 1 << 19,
        PlayerRagdoll = 1 << 20,
        MapHazards = 1 << 21,
        ScanNode = 1 << 22,
        EnemiesNotRendered = 1 << 23,
        MiscLevelGeometry = 1 << 24,
        Terrain = 1 << 25,
        PlaceableShipobjects = 1 << 26,
        PlacementBlocker = 1 << 27,
        Railing = 1 << 28
    }
    [HarmonyPatch]
    internal class PlayerControllerPatch
    {
        private static float pingInterval = 1f;

        private static RaycastHit pingHit;
        private static bool hasHit = false;
        private static PlayerControllerB __mainPlayer;
        private static bool initializing = true;

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void ReadInput(PlayerControllerB __instance)
        {
            try
            {
                if (PlayerControllerPatch.initializing) { return; }
                if ((object)__mainPlayer == (object)null)
                {
                    __mainPlayer = StartOfRound.Instance.localPlayerController;
                }
                if (__instance.IsOwner && !__mainPlayer.inTerminalMenu && !__mainPlayer.isTypingChat && !__mainPlayer.isPlayerDead && !__mainPlayer.quickMenuManager.isMenuOpen)
                {
                    if (Keyboard.current.tKey.wasPressedThisFrame && pingInterval <= 0f)
                    {
                        pingInterval = 0.25f;
                        LethalPingPlugin.mls.LogInfo("T Key Pressed!");
                        Vector3 startPos = __mainPlayer.gameplayCamera.transform.position + (__mainPlayer.gameplayCamera.transform.forward * .5f);

                        hasHit = Physics.BoxCast(startPos, new Vector3(0.25f, 0.25f, 0.25f), __mainPlayer.gameplayCamera.transform.forward, out pingHit, __mainPlayer.gameplayCamera.transform.rotation, 75f, (int)GameLayers.Enemies);
                        if (hasHit)
                        {
                            LethalPingPlugin.mls.LogInfo("Boxcast object was hit!");
                            LethalPingPlugin.mls.LogInfo($"Object name hit: {pingHit.collider.gameObject.name}");
                            LethalPingPlugin.mls.LogInfo($"Object position hit: {pingHit.point}");
                            LethalPingPlugin.mls.LogInfo($"Object layer hit: {pingHit.collider.gameObject.layer}");
                            LethalPingPlugin.mls.LogInfo($"Scannode properties (if any): {GetHeaderText(pingHit)}");

                            ulong localClientId = GameNetworkManager.Instance.localPlayerController.playerClientId;

                            /*int playerNum = 0;
                            if (!__instance.isHostPlayerObject)
                            {
                                playerNum = GetPlayerNum(GameNetworkManager.Instance.localPlayerController.playerClientId);
                            }*/
                            if (LethalPingPlugin.objPings.Value)
                            {
                                PingController.Instance.setObjectPing(pingHit.collider.transform.root.gameObject, GetPingLifetime(pingHit), StartOfRound.Instance.allPlayerScripts[localClientId].playerUsername, GetHeaderText(pingHit), GetNodeType(pingHit), localClientId);
                            }
                            else
                            {
                                PingController.Instance.setLocationPing(pingHit.point, GetPingLifetime(pingHit), StartOfRound.Instance.allPlayerScripts[localClientId].playerUsername, GetHeaderText(pingHit), GetNodeType(pingHit), localClientId);
                            }

                            //HUDManagerPatch.pingHits[playerNum] = pingHit;
                            //HUDManagerPatch.pingTimes[localClientId] = Time.time;
                            if (HUDManagerPatch.pingElements[localClientId].gameObject.activeSelf)
                            {
                                HUDManagerPatch.pingElements[localClientId].gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            if (LethalPingPlugin.plyPings.Value)
                            {
                                hasHit = Physics.BoxCast(startPos, new Vector3(0.1f, 0.1f, 0.1f), __mainPlayer.gameplayCamera.transform.forward, out pingHit, __mainPlayer.gameplayCamera.transform.rotation, 30f, (int)GameLayers.Player);
                                if (hasHit)
                                {
                                    ulong localCLientId = GameNetworkManager.Instance.localPlayerController.playerClientId;
                                    if (LethalPingPlugin.objPings.Value)
                                    {
                                        PingController.Instance.setObjectPing(pingHit.collider.gameObject, LethalPingPlugin.GetCurTime() + LethalPingPlugin.playerTime.Value, StartOfRound.Instance.allPlayerScripts[localCLientId].playerUsername, ((object)pingHit.collider.gameObject.GetComponent<NetworkObject>() != (object)null ? StartOfRound.Instance.allPlayerScripts[pingHit.collider.gameObject.GetComponent<NetworkObject>().OwnerClientId].playerUsername : "new phone who dis"), 2, localCLientId);
                                    } else
                                    {
                                        PingController.Instance.setLocationPing(pingHit.point, LethalPingPlugin.GetCurTime() + LethalPingPlugin.playerTime.Value, StartOfRound.Instance.allPlayerScripts[localCLientId].playerUsername, ((object)pingHit.collider.gameObject.GetComponent<NetworkObject>() != (object)null ? StartOfRound.Instance.allPlayerScripts[pingHit.collider.gameObject.GetComponent<NetworkObject>().OwnerClientId].playerUsername : "new phone who dis"), 2, localCLientId);
                                    }
                                    if (HUDManagerPatch.pingElements[localCLientId].gameObject.activeSelf)
                                    {
                                        HUDManagerPatch.pingElements[localCLientId].gameObject.SetActive(false);
                                    }
                                }
                            }
                            if (LethalPingPlugin.plyPings.Value && !hasHit)
                            {
                                GameLayers mask = GameLayers.Player | GameLayers.Props | GameLayers.Room | GameLayers.InteractableObject | GameLayers.PhysicsObject | GameLayers.Anomaly | GameLayers.Enemies | GameLayers.PlayerRagdoll | GameLayers.Terrain;
                                hasHit = Physics.Raycast(__mainPlayer.gameplayCamera.transform.position + (__mainPlayer.gameplayCamera.transform.forward * .5f), __mainPlayer.gameplayCamera.transform.forward, out pingHit, 100f, (int)mask);
                                if (hasHit)
                                {
                                    LethalPingPlugin.mls.LogInfo("Raycast object was hit!");
                                    LethalPingPlugin.mls.LogInfo($"Object name hit: {pingHit.collider.gameObject.name}");
                                    LethalPingPlugin.mls.LogInfo($"Object position hit: {pingHit.point}");
                                    LethalPingPlugin.mls.LogInfo($"Object layer hit: {pingHit.collider.gameObject.layer}");
                                    LethalPingPlugin.mls.LogInfo($"Scannode properties (if any): {GetHeaderText(pingHit)}");

                                    ulong localClientId = GameNetworkManager.Instance.localPlayerController.playerClientId;
                                    /*int playerNum = 0;
                                    if (!__instance.isHostPlayerObject)
                                    {
                                        playerNum = GetPlayerNum(GameNetworkManager.Instance.localPlayerController.playerClientId);
                                    }*/
                                    PingController.Instance.setLocationPing(pingHit.point, GetPingLifetime(pingHit), StartOfRound.Instance.allPlayerScripts[localClientId].playerUsername, GetHeaderText(pingHit), GetNodeType(pingHit), localClientId);

                                    //HUDManagerPatch.pingHits[localClientId] = pingHit;
                                    //HUDManagerPatch.pingTimes[localClientId] = Time.time;
                                    if (HUDManagerPatch.pingElements[localClientId].gameObject.activeSelf)
                                    {
                                        HUDManagerPatch.pingElements[localClientId].gameObject.SetActive(false);
                                        PingController.Instance.pings[localClientId].isAttachedToObj = false;
                                    }
                                }
                            }
                        }
                    }
                }
                pingInterval -= Time.deltaTime;
            }
            catch { }
        }

        public static ScanNodeProperties getScanNodeProperties(RaycastHit pingHit)
        {
            if (pingHit.collider.gameObject.GetComponentInChildren<ScanNodeProperties>() != null)
            {
                return pingHit.collider.gameObject.GetComponentInChildren<ScanNodeProperties>();
            }
            else if (pingHit.transform.root.gameObject.GetComponentInChildren<ScanNodeProperties>() != null && pingHit.transform.root.gameObject.name != "Environment")
            {
                return pingHit.transform.root.gameObject.GetComponentInChildren<ScanNodeProperties>();
            }
            return null;
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPostfix()]
        public static void Uninitialize()
        {
            __mainPlayer = null;
            initializing = true;
        }

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix()]
        public static void Initialize()
        {
            //__mainPlayer = StartOfRound.Instance.localPlayerController;
            initializing = false;
        }

        public static double GetPingLifetime(RaycastHit pingHit)
        {
            /* Nodetypes:
             * 
             * 0: Default (Clipboard, main entrance, etc) - shortest ping time, 10s;
             * 1: Enemy - longest ping time, 30s;
             * 2: Scrap - middling ping time, 15s;
             * if no node: lifetime is 10s;
             * */
            double curTime = LethalPingPlugin.GetCurTime();
            double lifetime = curTime + LethalPingPlugin.genericTime.Value;
            ScanNodeProperties properties = getScanNodeProperties(pingHit);
            if ((object)properties != (object)null)
            {
                if (properties.nodeType == 1)
                {
                    lifetime = curTime + LethalPingPlugin.enemyTime.Value;
                }
                else if (properties.nodeType == 2)
                {
                    lifetime = curTime + LethalPingPlugin.scrapTime.Value;
                }
            }
            return lifetime;
        }

        public static string GetHeaderText(RaycastHit pingHit)
        {
            String text = "Generic";
            ScanNodeProperties properties = getScanNodeProperties(pingHit);
            if ((object)properties != (object)null)
            {
                text = properties.headerText;
            }
            return text;
        }

        public static int GetNodeType(RaycastHit pingHit)
        {
            int nodeType = 0;
            ScanNodeProperties properties = getScanNodeProperties(pingHit);
            if ((object)properties != (object)null)
            {
                nodeType = properties.nodeType;
            }
            return nodeType;
        }

        /*public static int GetPlayerNum(ulong playerId)
        {
            int num = 0;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (playerId == StartOfRound.Instance.allPlayerScripts[i].actualClientId)
                {
                    num = i;
                }
            }
            return num;
        }*/

        /*public static int GetPlayerNum(GameObject playerObj)
        {
            for(int i =0;i<LethalPingPlugin.PlayerCount;i++)
            {
                if(playerObj == StartOfRound.Instance.allPlayerObjects[i])
                {
                    return i;
                }
            }
            return LethalPingPlugin.PlayerCount+1;
        }*/
    }
}
