using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
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
    [BepInDependency("LC_API", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
    public class LethalPingPlugin : BaseUnityPlugin
    {

        private const string pluginGUID = "com.greyull.lethalping";
        private const string pluginName = "Lethal Ping";
        private const string pluginVersion = "1.2.2";

        //public static int PlayerCount = 50;

        //public static PingElement[] allPings;
        public static PingController pingController;

        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(pluginGUID);

        public static bool initializing = true;

        public static ConfigFile config;
        public static ConfigEntry<Boolean> objPings;
        public static ConfigEntry<Boolean> plyPings;
        public static ConfigEntry<Double> genericTime;
        public static ConfigEntry<Double> scrapTime;
        public static ConfigEntry<Double> enemyTime;
        public static ConfigEntry<Double> playerTime;
        public static ConfigEntry<Boolean> debugLogs;

        private Harmony _harmony = new Harmony(pluginGUID);

        private void Awake()
        {
            //Config Bindings
            config = base.Config;
            objPings = config.Bind<Boolean>(new ConfigDefinition("General", "Object Pings Enabled"), true, new ConfigDescription("Whether or not object-type pings will be enabled, allowing ping position to move with the tagged object."));
            plyPings = config.Bind<Boolean>(new ConfigDefinition("General", "Player Pings Enabled"), true, new ConfigDescription("Whether or not player-type pings will be enabled, allowing you to ping other players."));
            genericTime = config.Bind<Double>(new ConfigDefinition("Timings", "Generic Ping Duration"), (double)10, new ConfigDescription("Time in seconds that generic pings will last for."));
            scrapTime = config.Bind<Double>(new ConfigDefinition("Timings", "Scrap Ping Duration"), (double)15, new ConfigDescription("Time in seconds that scrap pings will last for."));
            enemyTime = config.Bind<Double>(new ConfigDefinition("Timings", "Enemy Ping Duration"), (double)30, new ConfigDescription("Time in seconds that enemy pings will last for."));
            playerTime = config.Bind<Double>(new ConfigDefinition("Timings", "Player Ping Duration"), (double)15, new ConfigDescription("Time in seconds that player pings will last for."));
            debugLogs = config.Bind<Boolean>(new ConfigDefinition("Debug", "Debug Logging Enabled"), false, new ConfigDescription("Whether or not debug logs should be printed to console."));

            //PlayerCount = GetPlayerCount();
            mls.LogInfo("LethalPing Plugin Loaded");

            //Harmony Patch operations
            _harmony.PatchAll(typeof(LethalPingPlugin));
            _harmony.PatchAll(typeof(HUDManagerPatch));
            _harmony.PatchAll(typeof(PlayerControllerPatch));
            _harmony.PatchAll(typeof(PingController));
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
                    if (LethalPingPlugin.debugLogs.Value) mls.LogMessage("No larger lobby mods detected, playercount kept at default of 4...");
                    return 4;
                }
            }
        }

        //TypeLoads occur on method execution, so try/catch needs to exist outside of method.
        private int CheckForMoreCompany()
        {
            //No logic needed to check if moreCompany exists, this method will pass TypeLoadException if MoreCompany doesn't exist.
            if (LethalPingPlugin.debugLogs.Value) mls.LogMessage($"MoreCompany detected, adjusting playercount to MoreCompany count {MainClass.newPlayerCount}...");
            return MainClass.newPlayerCount;
        }

        private int CheckForBiggerLobby()
        {
            //No logic needed to check if BiggerLobby exists, this method will pass TypeLoadException if BiggerLobby doesn't exist.
            if (LethalPingPlugin.debugLogs.Value) mls.LogMessage($"BiggerLobby detected, adjusting playercount to BiggerLobby count {Plugin.MaxPlayers}...");
            return Plugin.MaxPlayers;
        }

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPrefix()]
        private static void SpawnElements()
        {
            if (initializing)
            {
                pingController = new PingController();
                initializing = false;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPostfix()]
        private static void uninitialize()
        {
            initializing = true;
            pingController = null;
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
