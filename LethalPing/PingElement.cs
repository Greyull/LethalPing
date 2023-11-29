using LC_API.ServerAPI;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using LethalPing.Patches;

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

    public class PingElement
    {
        // TODO: Create PingElement NetworkBehaviour, instead of referring to pingHit field use pingElements (maybe contained in a static field array in LethalPingPlugin?)

        public string pingUsername;
        public string pingText;
        public double pingLifetime;
        public int nodeType;
        public bool isAttachedToObj = false;
        public int ObjHash;
        public GameObject attachedNode;
        public Vector3 pingPosition;
        public int pingNum;

        public PingElement()
        {
            handleIncomingPing();
        }

        private void handleIncomingPing()
        {
            Networking.GetString = delegate (string message, string signature)
            {
                LethalPingPlugin.mls.LogInfo("Received message: " + message);
                if (signature.Equals("location_ping"))
                {
                    LocationPing pingData = JsonConvert.DeserializeObject<LocationPing>(message);
                    if (pingData == null)
                    {
                        LethalPingPlugin.mls.LogWarning("Failed to parse ping data");
                    }
                    else if (pingData.clientId == (ulong)this.pingNum)
                    {
                        LethalPingPlugin.mls.LogMessage($"Received ping from {pingData.pingUsername} containing {pingData.pingText} at {pingData.pingPosition.ToString()} until {pingData.pingLifetime}");
                        this.pingUsername = pingData.pingUsername;
                        this.pingText = pingData.pingText;
                        this.pingPosition = pingData.pingPosition;
                        this.pingLifetime = pingData.pingLifetime;
                        this.nodeType = pingData.nodeType;
                        this.isAttachedToObj = false;

                        HUDManagerPatch.pingElements[this.pingNum].gameObject.SetActive(false);
                    }
                }
                else if (signature.Equals("object_ping"))
                {
                    ObjectPing pingData = JsonConvert.DeserializeObject<ObjectPing>(message);
                    if (pingData == null)
                    {
                        LethalPingPlugin.mls.LogWarning("Failed to parse ping data");
                    }
                    else if (pingData.clientId == (ulong)this.pingNum)
                    {
                        LethalPingPlugin.mls.LogMessage($"Received ping from {pingData.pingUsername} containing {pingData.pingText} referencing objectId {pingData.networkObjId} until {pingData.pingLifetime}");
                        this.pingUsername = pingData.pingUsername;
                        this.pingText = pingData.pingText;
                        this.pingLifetime = pingData.pingLifetime;
                        this.nodeType = pingData.nodeType;
                        this.isAttachedToObj = pingData.networkObjId == 0 ? false : true;
                        this.attachedNode = pingData.networkObjId == 0 ? null : findObjectById(pingData.networkObjId);

                        HUDManagerPatch.pingElements[this.pingNum].gameObject.SetActive(false);
                    }
                }
            };
        }

        public void setLocationPing(Vector3 position, double lifetime, string username, string text, int nodeType, ulong clientId)
        {
            this.pingPosition = position;
            this.pingLifetime = lifetime;
            this.pingUsername = username;
            this.pingText = text;
            this.nodeType = nodeType;
            Networking.Broadcast(JsonConvert.SerializeObject(new LocationPing
            {
                pingPosition = this.pingPosition,
                pingLifetime = this.pingLifetime,
                pingUsername = this.pingUsername,
                pingText = this.pingText,
                nodeType = this.nodeType,
                clientId = clientId
            }, Formatting.None, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }), "location_ping");
            LethalPingPlugin.mls.LogMessage($"Sending ping at {this.pingPosition.ToString()} until {pingLifetime}");
        }

        public void setObjectPing(GameObject refObj, double lifetime, string username, string text, int nodeType, ulong clientId)
        {
            this.pingUsername = username;
            this.pingText = text;
            this.pingLifetime = lifetime;
            this.attachedNode = refObj;
            this.isAttachedToObj = true;
            this.nodeType = nodeType;
            Networking.Broadcast(JsonConvert.SerializeObject(new ObjectPing
            {
                pingLifetime = this.pingLifetime,
                pingUsername = this.pingUsername,
                pingText = this.pingText,
                networkObjId = (refObj.GetComponent<NetworkObject>() != null) ? refObj.GetComponent<NetworkObject>().NetworkObjectId : 0,
                nodeType = this.nodeType,
                clientId = clientId
            }), "object_ping");
            LethalPingPlugin.mls.LogMessage($"Sending object ping for object {((refObj.GetComponent<NetworkObject>() != null) ? refObj.GetComponent<NetworkObject>().NetworkObjectId : 0)} until {pingLifetime}");
        }

        public static GameObject findObjectById(ulong networkId)
        {
            if (networkId == 0) return null;
            return NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkId].gameObject;
        }
    }
}
