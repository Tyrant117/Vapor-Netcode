using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public class NetManager : MonoBehaviour
    {
        public static NetManager Instance;

        [SerializeField]
        private bool _isClient;
        [SerializeField]
        private ClientConfig _clientConfig;

        [SerializeField]
        private bool _isServer;
        [SerializeField]
        private ServerConfig _serverConfig;

        private void OnValidate()
        {
            var serverMods = transform.Find("Server Modules");
            if(serverMods == null)
            {
                var go = new GameObject("Server Modules");
                go.transform.SetParent(transform, false);
            }

            var clientMods = transform.Find("Client Modules");
            if (clientMods == null)
            {
                var go = new GameObject("Client Modules");
                go.transform.SetParent(transform, false);
            }
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_isServer)
            {
                var serverMods = GetComponentsInChildren<ServerModule>();
                UDPServer.Listen(_serverConfig, UDPServer.GeneratePeer, serverMods);
            }

            if (_isClient)
            {
                var clientMods = GetComponentsInChildren<ClientModule>();
                UDPClient.Initialize(_clientConfig, UDPClient.GeneratePeer, clientMods);
                if (_clientConfig.connectOnStart)
                {
                    StartCoroutine(WaitToAutoConnect());
                }
            }
        }

        private IEnumerator WaitToAutoConnect()
        {
            yield return new WaitForSeconds(0.2f);
            UDPClient.Connect(_clientConfig.gameServerIp, _clientConfig.gameServerPort);
        }
    }
}
