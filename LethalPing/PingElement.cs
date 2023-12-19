using LC_API.ServerAPI;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using LethalPing.Patches;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace LethalPing
{
    [JsonObject]
    internal class LocationPing
    {
        [JsonProperty]
        public string pingUsername { get; set; }
        [JsonProperty]
        public string pingText { get; set; }
        [JsonProperty]
        public Vector3 pingPosition { get; set; }
        [JsonProperty]
        public double pingLifetime { get; set; }
        [JsonProperty]
        public int nodeType { get; set; }
        [JsonProperty]
        public ulong clientId { get; set; }
    }

    [JsonObject]
    internal class ObjectPing
    {
        [JsonProperty]
        public string pingUsername { get; set; }
        [JsonProperty]
        public string pingText { get; set; }
        [JsonProperty]
        public ulong networkObjId { get; set; }
        [JsonProperty]
        public double pingLifetime { get; set; }
        [JsonProperty]
        public int nodeType { get; set; }
        [JsonProperty]
        public ulong clientId { get; set; }
    }

    public class PingController
    {
        // TODO: Create PingElement NetworkBehaviour, instead of referring to pingHit field use pingElements (maybe contained in a static field array in LethalPingPlugin?)
        public Dictionary<ulong, PingElement> pings = new Dictionary<ulong, PingElement>();

        /*public string[] pingUsername;
        public string[] pingText;
        public double[] pingLifetime;
        public int[] nodeType;
        public bool[] isAttachedToObj;
        public int[] ObjHash;
        public GameObject[] attachedNode;
        public Vector3[] pingPosition;*/

        public static PingController Instance { get; private set; }

        public PingController()
        {
            Instance = this;
            /*pingUsername = new string[LethalPingPlugin.PlayerCount];
            pingText = new string[LethalPingPlugin.PlayerCount];
            pingLifetime = new double[LethalPingPlugin.PlayerCount];
            nodeType = new int[LethalPingPlugin.PlayerCount];
            isAttachedToObj = new bool[LethalPingPlugin.PlayerCount];
            for (int i = 0; i < isAttachedToObj.Length; i++)
            {
                isAttachedToObj[i] = false;
            }
            ObjHash = new int[LethalPingPlugin.PlayerCount];
            attachedNode = new GameObject[LethalPingPlugin.PlayerCount];
            pingPosition = new Vector3[LethalPingPlugin.PlayerCount];*/
            handleIncomingPing();
        }

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix()]
        public static void CreatePingElements(StartOfRound __instance)
        {
            if (LethalPingPlugin.debugLogs.Value) LethalPingPlugin.mls.LogInfo("Creating PingElements...");
            for (int i = 0; i < GameNetworkManager.Instance.currentLobby.Value.MaxMembers; i++)
            {
                PingController.Instance.pings[Convert.ToUInt64(i)] = new PingElement();
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPrefix()]
        public static void Uninstantiate()
        {
            PingController.Instance.pings = new Dictionary<ulong, PingElement>();
        }

        [HarmonyPatch(typeof(SceneManager), "LoadScene")]
        [HarmonyPostfix()]
        public static void clearPingsOnSceneChange()
        {
            foreach (KeyValuePair<ulong, PingElement> p in PingController.Instance.pings)
            {
                p.Value.pingLifetime = 0;
                HUDManagerPatch.pingElements[p.Key].gameObject.SetActive(false);
            }
        }

        /*[HarmonyPatch(typeof(StartOfRound), "OnClientConnect")]
        [HarmonyPrefix()]
        public static void AddScanElementOnServer(StartOfRound __instance, ulong clientId)
        {
            LethalPingPlugin.mls.LogInfo("OnClientConnect executed");
            if (__instance.IsServer)
            {
                PingController.Instance.pings[clientId] = new PingElement();
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "OnPlayerConnectedClientRpc")]
        [HarmonyPrefix()]
        public static void AddScanElementOnClient(StartOfRound __instance, ulong clientId)
        {
            LethalPingPlugin.mls.LogInfo("OnPlayerConnectedClientRpc executed");
            if (!__instance.IsServer)
            {
                PingController.Instance.pings[clientId] = new PingElement();
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "OnPlayerDC")]
        [HarmonyPrefix()]
        public static void RemoveScanElement(StartOfRound __instance, int playerObjectNumber, ulong clientId)
        {
            PingController.Instance.pings.Remove(clientId);
        }*/

        private void handleIncomingPing()
        {
            Networking.GetString = delegate (string message, string signature)
            {
                if (LethalPingPlugin.debugLogs.Value) LethalPingPlugin.mls.LogInfo($"{signature} Received message: {message}");
                if (signature.Equals("location_ping"))
                {
                    LocationPing pingData = JsonConvert.DeserializeObject<LocationPing>(message);
                    if (pingData == null)
                    {
                        if (LethalPingPlugin.debugLogs.Value) LethalPingPlugin.mls.LogWarning("Failed to parse ping data");
                    }
                    else
                    {
                        if (LethalPingPlugin.debugLogs.Value) LethalPingPlugin.mls.LogMessage($"Received ping from {pingData.pingUsername} containing {pingData.pingText} at {pingData.pingPosition.ToString()} until {pingData.pingLifetime}");
                        //this.pingUsername[pingData.clientId] = pingData.pingUsername;
                        this.pings[pingData.clientId].pingUsername = pingData.pingUsername;
                        //this.pingText[pingData.clientId] = pingData.pingText;
                        this.pings[pingData.clientId].pingText = pingData.pingText;
                        //this.pingPosition[pingData.clientId] = pingData.pingPosition;
                        this.pings[pingData.clientId].pingPosition = pingData.pingPosition;
                        //this.pingLifetime[pingData.clientId] = pingData.pingLifetime;
                        this.pings[pingData.clientId].pingLifetime = pingData.pingLifetime;
                        //this.nodeType[pingData.clientId] = pingData.nodeType;
                        this.pings[pingData.clientId].nodeType = pingData.nodeType;
                        //this.isAttachedToObj[pingData.clientId] = false;

                        HUDManagerPatch.pingElements[pingData.clientId].gameObject.SetActive(false);
                    }
                }
                else if (signature.Equals("object_ping"))
                {
                    ObjectPing pingData = JsonConvert.DeserializeObject<ObjectPing>(message);
                    if (pingData == null)
                    {
                        if (LethalPingPlugin.debugLogs.Value) LethalPingPlugin.mls.LogWarning("Failed to parse ping data");
                    }
                    else
                    {
                        if (LethalPingPlugin.debugLogs.Value) LethalPingPlugin.mls.LogMessage($"Received ping from {pingData.pingUsername} containing {pingData.pingText} referencing objectId {pingData.networkObjId} until {pingData.pingLifetime}");
                        //this.pingUsername[pingData.clientId] = pingData.pingUsername;
                        this.pings[pingData.clientId].pingUsername = pingData.pingUsername;
                        //this.pingText[pingData.clientId] = pingData.pingText;
                        this.pings[pingData.clientId].pingText = pingData.pingText;
                        //this.pingLifetime[pingData.clientId] = pingData.pingLifetime;
                        this.pings[pingData.clientId].pingLifetime = pingData.pingLifetime;
                        //this.nodeType[pingData.clientId] = pingData.nodeType;
                        this.pings[pingData.clientId].nodeType = pingData.nodeType;
                        //this.isAttachedToObj[pingData.clientId] = pingData.networkObjId == 0 ? false : true;
                        this.pings[pingData.clientId].isAttachedToObj = pingData.networkObjId == 0 ? false : true;
                        //this.attachedNode[pingData.clientId] = pingData.networkObjId == 0 ? null : findObjectById(pingData.networkObjId);
                        this.pings[pingData.clientId].attachedNode = pingData.networkObjId == 0 ? null : findObjectById(pingData.networkObjId);

                        HUDManagerPatch.pingElements[pingData.clientId].gameObject.SetActive(false);
                    }
                }
            };
        }

        public void setLocationPing(Vector3 position, double lifetime, string username, string text, int nodeType, ulong clientId)
        {
            //this.pingPosition[clientId] = position;
            this.pings[clientId].pingPosition = position;
            //this.pingLifetime[clientId] = lifetime;
            this.pings[clientId].pingLifetime = lifetime;
            //this.pingUsername[clientId] = username;
            this.pings[clientId].pingUsername = username;
            //this.pingText[clientId] = text;
            this.pings[clientId].pingText = text;
            //this.nodeType[clientId] = nodeType;
            this.pings[clientId].nodeType = nodeType;
            Networking.Broadcast(JsonConvert.SerializeObject(new LocationPing
            {
                pingPosition = this.pings[clientId].pingPosition,
                pingLifetime = this.pings[clientId].pingLifetime,
                pingUsername = this.pings[clientId].pingUsername,
                pingText = this.pings[clientId].pingText,
                nodeType = this.pings[clientId].nodeType,
                clientId = clientId
            }, Formatting.None, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }), "location_ping");
            if (LethalPingPlugin.debugLogs.Value) LethalPingPlugin.mls.LogMessage($"Sending ping at {this.pings[clientId].pingPosition.ToString()} until {this.pings[clientId].pingLifetime}");
        }

        public void setObjectPing(GameObject refObj, double lifetime, string username, string text, int nodeType, ulong clientId)
        {
            //this.pingUsername[clientId] = username;
            this.pings[clientId].pingUsername = username;
            //this.pingText[clientId] = text;
            this.pings[clientId].pingText = text;
            //this.pingLifetime[clientId] = lifetime;
            this.pings[clientId].pingLifetime = lifetime;
            //this.attachedNode[clientId] = refObj;
            this.pings[clientId].attachedNode = refObj;
            //this.isAttachedToObj[clientId] = true;
            this.pings[clientId].isAttachedToObj = true;
            //this.nodeType[clientId] = nodeType;
            this.pings[clientId].nodeType = nodeType;
            Networking.Broadcast(JsonConvert.SerializeObject(new ObjectPing
            {
                pingLifetime = this.pings[clientId].pingLifetime,
                pingUsername = this.pings[clientId].pingUsername,
                pingText = this.pings[clientId].pingText,
                networkObjId = (refObj.GetComponent<NetworkObject>() != null) ? refObj.GetComponent<NetworkObject>().NetworkObjectId : 0,
                nodeType = this.pings[clientId].nodeType,
                clientId = clientId
            }), "object_ping");
            if (LethalPingPlugin.debugLogs.Value) LethalPingPlugin.mls.LogMessage($"Sending object ping for object {((refObj.GetComponent<NetworkObject>() != null) ? refObj.GetComponent<NetworkObject>().NetworkObjectId : 0)} until {lifetime}");

        }

        public static GameObject findObjectById(ulong networkId)
        {
            if (networkId == 0) return null;
            return NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkId].gameObject;
        }

        public static PingElement getPing(ulong num)
        {
            return Instance.pings[num];
        }
    }
    public class PingElement
    {
        public string pingUsername { get; set; }
        public string pingText { get; set; }
        public double pingLifetime { get; set; }
        public int nodeType { get; set; }
        public bool isAttachedToObj { get; set; }
        public int ObjHash { get; set; }
        public GameObject attachedNode { get; set; }
        public Vector3 pingPosition { get; set; }
    }
}

