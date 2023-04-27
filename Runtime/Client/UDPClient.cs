using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace VaporNetcode
{
    [Serializable]
    public class ClientConfig
    {
        #region Inspector
#if ODIN_INSPECTOR
        [FoldoutGroup("Properties")]
#else
        [Header("Properties")]
#endif
        [Tooltip("Should client connect by itself.")]
        public bool AutoConnect;

#if ODIN_INSPECTOR
        [FoldoutGroup("Properties")]
#endif
        [Tooltip("Address to the server")]
        public string GameServerIp = "127.0.0.1";

#if ODIN_INSPECTOR
        [FoldoutGroup("Properties")]
#endif
        [Tooltip("Port of the server")]
        public int GameServerPort = 7777;

#if ODIN_INSPECTOR
        [FoldoutGroup("Properties")]
#endif
        [Tooltip("Client Target Send Rate")]
        public int ClientUpdateRate = 30;

#if ODIN_INSPECTOR
        [FoldoutGroup("Properties")]
#else
        [Header("Debug")]
#endif
        public bool IsSimulated;
        #endregion
    }

    public static class UDPClient
    {
        public const string TAG = "<color=cyan><b>[Client]</b></color>";        

        private static bool isInitialized;
        private static bool isSimulated;

        private static readonly bool retryOnTimeout = true;
        public static float SendInterval => 1f / _config.ClientUpdateRate; // for 30 Hz, that's 33ms
        private static double _lastSendTime;

        private static ClientConfig _config;

        #region Connections
        private static int connectionID = -1;
        private static float stopConnectingTime;
        private static bool isAttemptingReconnect;

        public static Peer ServerPeer { get; private set; }

        private static readonly float pingFrequency = 2.0f;
        private static double lastPingTime;
        public static double LocalTimeline { get; private set; }
        #endregion

        #region Modules
        private static Dictionary<Type, ClientModule> modules; // Modules added to the network manager
        private static HashSet<Type> initializedModules; // set of initialized modules on the network manager
        #endregion

        #region Messaging
        private static Dictionary<ushort, IPacketHandler> handlers; // key value pair to handle messages.
        #endregion

        #region Current Connection
        private static ConnectionStatus status;
        /// <summary>
        ///     Current connections status. If changed invokes StatusChanged event./>
        /// </summary>
        public static ConnectionStatus Status
        {
            get { return status; }
            set
            {
                if (status != value && StatusChanged != null)
                {
                    status = value;
                    StatusChanged.Invoke(status);
                    return;
                }
                status = value;
            }
        }

        public static bool IsActive => status == ConnectionStatus.Connecting || status == ConnectionStatus.Connected;

        /// <summary>
        ///     True if we are connected to another socket
        /// </summary>
        public static bool IsConnected { get; private set; }

        /// <summary>
        ///     True if we are trying to connect to another socket
        /// </summary>
        public static bool IsConnecting { get; private set; }

        /// <summary>
        ///     IP Address of the connection
        /// </summary>
        public static string ConnectionIP { get; private set; }

        /// <summary>
        ///     Port of the connection
        /// </summary>
        public static int ConnectionPort { get; private set; }
        #endregion

        #region Event Handling
        /// <summary>
        ///     Event is invoked when we successfully connect to another socket.
        /// </summary>
        public static event Action Connected;

        /// <summary>
        ///     Event is invoked when we are disconnected from another socket.
        /// </summary>
        public static event Action Disconnected;

        /// <summary>
        ///     Event is invoked when the connection status changes.
        /// </summary>
        public static event Action<ConnectionStatus> StatusChanged;

        private static Func<int, UDPTransport.Source, Peer> PeerCreator;
        #endregion

        #region - Unity Methods and Initialization -

        public static void Initialize(ClientConfig config, Func<int, UDPTransport.Source, Peer> peerCreator, params ClientModule[] startingModules)
        {
            isInitialized = false;

            _config = config;            
            isSimulated = _config.IsSimulated;

            handlers = new Dictionary<ushort, IPacketHandler>();
            modules = new Dictionary<Type, ClientModule>();
            initializedModules = new HashSet<Type>();

            PeerCreator = peerCreator;
            Connected += OnConnected;
            Disconnected += OnDisconnected;

            foreach (var mod in startingModules)
            {
                AddModule(mod);
            }
            InitializeModules();

            UDPTransport.OnClientConnected = HandleConnect;
            UDPTransport.OnClientDataReceived = HandleData;
            UDPTransport.OnClientDisconnected = HandleDisconnect;
            UDPTransport.OnClientError = HandleTransportError;

            RegisterHandler<NetworkPongMessage>(NetworkTime.OnClientPong, false);


            UDPTransport.Init(false, true, isSimulated);
            isInitialized = true;            
        }

        internal static void NetworkEarlyUpdate()
        {
            // process all incoming messages first before updating the world
            if (isInitialized)
            {
                if (IsConnecting && !IsConnected)
                {
                    // Attempt Connection
                    if (Time.time > stopConnectingTime)
                    {
                        StopConnecting(true);
                        return;
                    }
                    Status = ConnectionStatus.Connecting;
                }
            }

            UDPTransport.ClientEarlyUpdate();
        }

        internal static void NetworkLateUpdate()
        {
            if(isInitialized && isSimulated)
            {
                while (UDPTransport.ReceiveSimulatedMessage(UDPTransport.Source.Client, out int connID, out UDPTransport.TransportEvent transportEvent, out ArraySegment<byte> data))
                {
                    switch (transportEvent)
                    {
                        case UDPTransport.TransportEvent.Connected:
                            HandleConnect();
                            break;
                        case UDPTransport.TransportEvent.Data:
                            HandleData(data, 1);
                            break;
                        case UDPTransport.TransportEvent.Disconnected:
                            HandleDisconnect();
                            break;
                    }
                }
            }

            if (IsActive)
            {
                if (!Application.isPlaying || AccurateInterval.Elapsed(NetworkTime.localTime, SendInterval, ref _lastSendTime))
                {
                    //Broadcast();
                }
            }

            if (IsConnected)
            {
                // update NetworkTime
                NetworkTime.UpdateClient();

                // update connection to flush out batched messages
                ServerPeer.Update();
            }

            UDPTransport.ClientLateUpdate();
        }
        #endregion

        #region - Module Methods -
        /// <summary>
        ///     Adds a network module to the manager.
        /// </summary>
        /// <param name="module"></param>
        private static void AddModule(ClientModule module)
        {
            if (modules.ContainsKey(module.GetType()))
            {
                if (NetLogFilter.logWarn) { Debug.Log(string.Format("{0} Module has already been added. {1} || ({2})", TAG, module, Time.time)); }
            }
            modules.Add(module.GetType(), module);
        }

        /// <summary>
        ///     Adds a network module to the manager and initializes all modules.
        /// </summary>
        /// <param name="module"></param>
        public static void AddModuleAndInitialize(ClientModule module)
        {
            AddModule(module);
            InitializeModules();
        }

        /// <summary>
        ///     Checks if the maanger has the module.
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public static bool HasModule(ClientModule module)
        {
            return modules.ContainsKey(module.GetType());
        }

        /// <summary>
        ///     Initializes all uninitialized modules
        /// </summary>
        /// <returns></returns>
        public static bool InitializeModules()
        {
            while (true)
            {
                var changed = false;
                foreach (var mod in modules)
                {
                    // Module is already initialized
                    if (initializedModules.Contains(mod.Key)) { continue; }

                    // Not all dependencies have been initialized. Wait until they are.
                    //if (!mod.Value.Dependencies.All(d => initializedModules.Any(d.IsAssignableFrom))) { continue; }

                    mod.Value.Initialize();
                    initializedModules.Add(mod.Key);
                    changed = true;
                }

                // If nothing else can be initialized
                if (!changed)
                {
                    return !GetUninitializedModules().Any();
                }
            }
        }

        /// <summary>
        ///     Gets the module of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetModule<T>() where T : ClientModule
        {
            modules.TryGetValue(typeof(T), out ClientModule module);
            if (module == null)
            {
                module = modules.Values.FirstOrDefault(m => m is T);
            }
            return module as T;
        }

        /// <summary>
        ///     Gets all initialized modules.
        /// </summary>
        /// <returns></returns>
        private static List<ClientModule> GetInitializedModules()
        {
            return modules
                .Where(m => initializedModules.Contains(m.Key))
                .Select(m => m.Value)
                .ToList();
        }

        /// <summary>
        ///     Gets all unitialized modules.
        /// </summary>
        /// <returns></returns>
        private static List<ClientModule> GetUninitializedModules()
        {
            return modules
                .Where(m => !initializedModules.Contains(m.Key))
                .Select(m => m.Value)
                .ToList();
        }
        #endregion

        #region - Connection Methods -
        /// <summary>
        ///     Starts connecting to another socket. Default timeout of 10s.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static void Connect(string ip, int port)
        {
            if (isInitialized)
            {
                Connect(ip, port, 10);
            }
            else
            {
                Debug.LogError("Client must be initialized before connecting");
            }
        }
        /// <summary>
        ///     Starts connecting to another socket with a specified timeout.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="timeout">Milliseconds</param>
        /// <returns></returns>
        private static void Connect(string ip, int port, int timeout)
        {
            connectionID = 1;
            stopConnectingTime = Time.time + timeout;
            ConnectionIP = ip;
            ConnectionPort = port;


            IsConnecting = true;
            if (!isSimulated)
            {
                UDPTransport.Connect(ip);
            }
            else
            {
                UDPTransport.SimulatedConnect(connectionID);
                HandleConnect();
            }
        }

        /// <summary>
        ///     Disconnect the <see cref="ClientSocket"/> from the <see cref="ServerSocket"/>.
        /// </summary>
        public static void Disconnect()
        {
            if (isSimulated)
            {
                UDPTransport.SimulateDisconnect(connectionID);
            }
            else
            {
                UDPTransport.Disconnect();
            }

            HandleDisconnect();
        }

        /// <summary>
        ///     Disconnects and attempts connecting again.
        /// </summary>
        public static void Reconnect()
        {
            isAttemptingReconnect = true;
            Disconnect();
            Connect(ConnectionIP, ConnectionPort);
        }

        /// <summary>
        ///     Stops trying to connect to the socket
        /// </summary>
        private static void StopConnecting(bool timedOut = false)
        {
            IsConnecting = false;
            Status = ConnectionStatus.Disconnected;
            if (timedOut && retryOnTimeout)
            {
                if (NetLogFilter.logInfo) { Debug.LogFormat("{2} Retrying to connect to server at || {0}:{1}", _config.GameServerIp, _config.GameServerPort, TAG); }
                Connect(_config.GameServerIp, _config.GameServerPort);
            }
        }

        private static void HandleConnect()
        {
            IsConnecting = false;
            IsConnected = true;

            NetworkTime.ResetStatics();

            Debug.Log($"[{TAG}] Connected");

            Status = ConnectionStatus.Connected;
            NetworkTime.UpdateClient();

            ServerPeer = PeerCreator.Invoke(connectionID, UDPTransport.Source.Client);

            Connected?.Invoke();
        }

        public static Peer GeneratePeer(int connectionID, UDPTransport.Source source)
        {
            return new Peer(connectionID, source)
            {
                IsConnected = true
            };
        }

        private static void HandleDisconnect()
        {
            Status = ConnectionStatus.Disconnected;
            IsConnected = false;
            connectionID = -1;

            if (ServerPeer != null)
            {
                ServerPeer.Dispose();
                ServerPeer.IsConnected = false;
            }
            Disconnected?.Invoke();
        }

        private static void OnDisconnected()
        {
            if (NetLogFilter.logInfo) { Debug.LogFormat("Disconnected from || {0}:{1}", _config.GameServerIp, _config.GameServerPort); }

            if (!isAttemptingReconnect)
            {

            }
            isAttemptingReconnect = false;
        }

        private static void OnConnected()
        {
            if (NetLogFilter.logInfo) { Debug.LogFormat("Connected to || {0}:{1}", _config.GameServerIp, _config.GameServerPort); }
        }
        #endregion

        #region - Handle Message Methods -
        private static void HandleTransportError(TransportError error, string reason)
        {
            Debug.LogWarning($"{TAG} Client Transport Error: {error}: {reason}.");
        }

        private static void HandleData(ArraySegment<byte> buffer, int channelID)
        {
            if (ServerPeer == null) { return; }

            if (!ServerPeer.Unbatcher.AddBatch(buffer))
            {
                Debug.LogWarning($"NetworkClient: failed to add batch, disconnecting.");
                Disconnect();
                return;
            }

            while (ServerPeer.Unbatcher.GetNextMessage(out var reader, out var remoteTimestamp))
            {
                if (reader.Remaining >= NetworkMessages.IdSize)
                {
                    ServerPeer.RemoteTimestamp = remoteTimestamp;
                    if (!PeerMessageReceived(reader, channelID))
                    {
                        Debug.LogWarning($"NetworkClient: failed to unpack and invoke message. Disconnecting.");
                        Disconnect();
                        return;
                    }
                }
                else
                {
                    // WARNING, not error. can happen if attacker sends random data.
                    Debug.LogWarning($"NetworkClient: received Message was too short (messages should start with message id)");
                    Disconnect();
                    return;
                }
            }

            if (ServerPeer.Unbatcher.BatchesCount > 0)
            {
                Debug.LogError($"Still had {ServerPeer.Unbatcher.BatchesCount} batches remaining after processing, even though processing was not interrupted by a scene change. This should never happen, as it would cause ever growing batches.\nPossible reasons:\n* A message didn't deserialize as much as it serialized\n*There was no message handler for a message id, so the reader wasn't read until the end.");
            }
        }

        /// <summary>
        ///     Called after the peer parses the message. Only assigned to the local player. Use <see cref="IncomingMessage.Sender"/> to determine what peer sent the message.
        /// </summary>
        /// <param name="msg"></param>
        private static bool PeerMessageReceived(NetworkReader reader, int channelId)
        {
            if (NetworkMessages.UnpackId(reader, out ushort opCode))
            {
                // try to invoke the handler for that message
                if (handlers.TryGetValue(opCode, out var handler))
                {
                    handler.Handle(ServerPeer, reader, channelId);

                    // message handler may disconnect client, making connection = null
                    // therefore must check for null to avoid NRE.
                    if (ServerPeer != null)
                    {
                        ServerPeer.LastMessageTime = Time.time;
                    }

                    return true;
                }
                else
                {
                    // => WARNING, not error. can happen if attacker sends random data.
                    Debug.LogWarning($"Unknown message id: {opCode}. This can happen if no handler was registered for this message.");
                    return false;
                }
            }
            else
            {
                // => WARNING, not error. can happen if attacker sends random data.
                Debug.LogWarning("Invalid message header.");
                return false;
            }
        }

        /// <summary>
        ///     Register a method handler
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="handlerMethod"></param>
        /// <returns></returns>
        public static void RegisterHandler<T>(IncomingMessageHandler<T> handler, bool requireAuthentication = true) where T : struct, INetMessage
        {
            ushort opCode = NetworkMessageId<T>.Id;
            if (handlers.Remove(opCode))
            {
                if (NetLogFilter.logInfo) { Debug.LogFormat("{0} Handler Overwritten", opCode); }
            }
            handlers.Add(opCode, new PacketHandler<T>(opCode, handler, requireAuthentication));
        }

        /// <summary>
        ///     Remove a specific message handler
        /// </summary>
        /// <param name="opCode"></param>
        /// <returns></returns>
        public static bool RemoveHandler(ushort opCode)
        {
            return handlers.Remove(opCode);
        }
        #endregion

        #region - Messaging Methods -
        /// <summary>
        ///     Sends a message to server.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="qos"></param>
        public static void Send<T>(T message, int channelId = Channels.Reliable) where T : struct, INetMessage
        {
            if (!IsConnected) { return; }

            using var w = NetworkWriterPool.Get();
            NetworkMessages.Pack(message, w);
            ArraySegment<byte> segment = w.ToArraySegment();
            if (!isSimulated)
            {
                ServerPeer.SendMessage(segment, channelId);
            }
            else
            {
                ServerPeer.SendSimulatedMessage(segment);
            }
            NetDiagnostics.OnSend(message, channelId, w.Position, 1);
        }

        public static int RegisterResponse(ResponseCallback callback, int timeout = 5)
        {
            return ServerPeer.RegisterResponse(callback, timeout);
        }
        #endregion
    }
}