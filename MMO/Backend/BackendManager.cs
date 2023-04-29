using System;
using UnityEngine;
using VaporNetcode;

namespace VaporMMO.Backend
{
    [DefaultExecutionOrder(-900)]
    public class BackendManager : MonoBehaviour
    {
        public enum Network
        {
            Client,
            Server
        }

        public static BackendManager Instance { get; private set; }

        #region Inspector
        [SerializeField]
        private BackendType backend;
        [SerializeField]
        private Network network;

        [SerializeField]
        private string worldServerBuildId;
        #endregion

        #region Login
        public string playfabID;
        public string playfabSessionTicket;

        public event Action<LoginInfoResult> LoggedIn;
        public Func<PlayFabLoginInfoResult> PollPlayFabLogin;

        public event Action<LoginRequestMessage, INetConnection, Action<INetConnection, LoginRequestMessage>> AuthenticateConnection;
        public void OnAuthenticateConnection(INetConnection conn, LoginRequestMessage packet, Action<INetConnection, LoginRequestMessage> callback)
        {
            if (AuthenticateConnection != null)
            {
                AuthenticateConnection?.Invoke(packet, conn, callback);
            }
            else
            {
                callback?.Invoke(conn, packet);
            }
        }

        private bool waitingForLoginResult;
        #endregion

        #region World
        /// <summary>
        /// BuildID, RegionID
        /// </summary>
        public event Action<string, string, Action<ServerInfoResult>> RequestWorldServer;
        public void OnRequestWorldServer(Action<ServerInfoResult> callback)
        {
            RequestWorldServer?.Invoke(worldServerBuildId, "", callback);
        }
        #endregion

        #region Database
        // Server Only
        public event Action<string, INetConnection, Action<INetConnection, GetAccountDataRequestMessage, AccountSpecifcation?>, Action<INetConnection, GetAccountDataRequestMessage, int>, Action<INetConnection, GetAccountDataRequestMessage, AccountDataSpecification?>> RequestPlayFabCharacterData;

        public void GetAccountAndCharacterData(string stringID, INetConnection conn, Action<INetConnection, GetAccountDataRequestMessage, AccountSpecifcation?> accountCallback, Action<INetConnection, GetAccountDataRequestMessage, int> characterCountCallback, Action<INetConnection, GetAccountDataRequestMessage, AccountDataSpecification?> characterCallback)
        {
            switch (backend)
            {
                case BackendType.Playfab:
                    RequestPlayFabCharacterData?.Invoke(stringID, conn, accountCallback, characterCountCallback, characterCallback);
                    break;
                case BackendType.Unity:
                    break;
                case BackendType.Unisave:
                    break;
                case BackendType.Local:
                    break;
            }
        }
        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
            waitingForLoginResult = true;
        }

        private void Update()
        {
            GetLoginData();
        }

        private void GetLoginData()
        {
            if (!waitingForLoginResult) { return; }
            switch (backend)
            {
                case BackendType.Playfab:
                    var pfLogin = PollPlayFabLogin != null ? PollPlayFabLogin.Invoke() : default;
                    waitingForLoginResult = !pfLogin.completeResult;
                    if (!waitingForLoginResult)
                    {
                        playfabID = pfLogin.playfabId;
                        playfabSessionTicket = pfLogin.sessionTicket;
                        LoggedIn?.Invoke(new LoginInfoResult()
                        {
                            playfabID = playfabID,
                            playfabSessionTicket = playfabSessionTicket
                        });
                    }
                    break;
                case BackendType.Unisave:
                    break;
                case BackendType.Unity:
                    break;
            }
        }
    }
}
