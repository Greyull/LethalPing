using LC_API.ServerAPI;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

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
    }

    [JsonObject]
    internal class ObjectPing
    {
        [JsonProperty]
        public string pingUsername { get; set; }
        [JsonProperty]
        public string pingText { get; set; }
        [JsonProperty]
        public NetworkObjectReference ObjRef { get; set; }
        [JsonProperty]
        public double pingLifetime { get; set; }
        [JsonProperty]
        public int nodeType { get; set; }
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
                    else
                    {
                        LethalPingPlugin.mls.LogMessage($"Received ping from {pingData.pingUsername} containing {pingData.pingText} at {pingData.pingPosition.ToString()} until {pingData.pingLifetime}");
                        this.pingUsername = pingData.pingUsername;
                        this.pingText = pingData.pingText;
                        this.pingPosition = pingData.pingPosition;
                        this.pingLifetime = pingData.pingLifetime;
                        this.nodeType = pingData.nodeType;
                        this.isAttachedToObj = false;

                        //Add logic to disable HUD Element if already active so it replays animation.
                    }
                } else if (signature.Equals("object_ping"))
                {
                    ObjectPing pingData = JsonConvert.DeserializeObject<ObjectPing>(message);
                    if (pingData == null)
                    {
                        LethalPingPlugin.mls.LogWarning("Failed to parse ping data");
                    } else
                    {
                        LethalPingPlugin.mls.LogMessage($"Received ping from {pingData.pingUsername} containing {pingData.pingText} referencing object {pingData.ObjRef} until {pingData.pingLifetime}");
                        this.pingUsername = pingData.pingUsername;
                        this.pingText = pingData.pingText;
                        this.pingLifetime = pingData.pingLifetime;
                        this.nodeType = pingData.nodeType;
                        NetworkObject obj;
                        this.isAttachedToObj = pingData.ObjRef.TryGet(out obj, StartOfRound.Instance.NetworkManager);
                        this.attachedNode = obj.gameObject;
                    }
                }
            };
        }

        public void setLocationPing(Vector3 position, double lifetime, string username, string text, int nodeType)
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
                nodeType = this.nodeType
            }, Formatting.None, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }), "location_ping");
            LethalPingPlugin.mls.LogMessage($"Sending ping at {this.pingPosition.ToString()} until {pingLifetime}");
        }

        public void setObjectPing(GameObject refObj, double lifetime, string username, string text, int nodeType)
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
                ObjRef = new NetworkObjectReference(refObj),
                nodeType = this.nodeType
            }), "object_ping");
            LethalPingPlugin.mls.LogMessage($"Sending object ping for object {new NetworkObjectReference(refObj).ToString()} until {pingLifetime}");
        }

        public static GameObject findObjectByName(string objName)
        {
            return GameObject.Find(objName);
        }
    }
}
