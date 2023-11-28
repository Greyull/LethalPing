using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine.InputSystem;
using UnityEngine;
using System.Diagnostics;
using LethalPing.Patches;
using System.Collections.Generic;
using MoreCompany;
using BiggerLobby;
using System.Reflection;
using System;
using Unity.Netcode;

namespace LethalPing
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    [BepInDependency("me.swipez.melonloader.morecompany", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("BiggerLobby", BepInDependency.DependencyFlags.SoftDependency)]
    public class LethalPingPlugin : BaseUnityPlugin
    {

        private const string pluginGUID = "com.greyull.lethalping";
        private const string pluginName = "Lethal Ping";
        private const string pluginVersion = "0.0.1";

        public static int PlayerCount = 4;

        public static PingElement[] allPings;

        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(pluginGUID);

        public static bool initializing = true;

        private Harmony _harmony = new Harmony(pluginGUID);

        private void Awake()
        {
            PlayerCount = GetPlayerCount();
            mls.LogInfo("LethalPing Plugin Loaded");
            _harmony.PatchAll(typeof(LethalPingPlugin));
            _harmony.PatchAll(typeof(HUDManagerPatch));
            _harmony.PatchAll(typeof(PlayerControllerPatch));
            _harmony.PatchAll(typeof(PingElement));
        }

        private int GetPlayerCount()
        {
            try
            {
                return CheckForMoreCompany();
            }
            catch (TypeLoadException)
            {
                try
                {
                    return CheckForBiggerLobby();
                }
                catch (TypeLoadException)
                {
                    mls.LogMessage("No larger lobby mods detected, playercount kept at default of 4...");
                    return 4;
                }
            }
        }

        //TypeLoads occur on method execution, so try/catch needs to exist outside of method.
        private int CheckForMoreCompany()
        {
            //No logic needed to check if moreCompany exists, this method will pass TypeLoadException if MoreCompany doesn't exist.
            mls.LogMessage($"MoreCompany detected, adjusting playercount to MoreCompany count {MainClass.newPlayerCount}...");
            return MainClass.newPlayerCount;
        }

        private int CheckForBiggerLobby()
        {
            //No logic needed to check if BiggerLobby exists, this method will pass TypeLoadException if BiggerLobby doesn't exist.
            mls.LogMessage($"BiggerLobby detected, adjusting playercount to BiggerLobby count {Plugin.MaxPlayers}...");
            return Plugin.MaxPlayers;
        }

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPrefix()]
        private static void SpawnElements()
        {
            if (initializing)
            {
                allPings = new PingElement[PlayerCount];
                for (int i = 0; i < PlayerCount; i++)
                {
                    allPings[i] = new PingElement();
                }
                initializing = false;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPostfix()]
        private static void uninitialize()
        {
            initializing = true;
            allPings = null;
        }

        public static double GetCurTime()
        {
            return DateTime.UtcNow.ToOADate() * 86400;
        }

        /*[HarmonyPatch(typeof(StartOfRound), "OnClientConnect")]
        [HarmonyPostfix()]
        private static void setElementOwnership(ulong clientId)
        {
            if (initializing)
            {
                return;
            }
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                return;
            }
            int num = 0;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (clientId == StartOfRound.Instance.allPlayerScripts[i].actualClientId)
                {
                    num = i;
                }
            }
            allPings[num].gameObject.GetComponent<NetworkObject>().ChangeOwnership(clientId);
        }*/
    }
}
