using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporMMO.Backend;
using VaporNetcode;

namespace VaporMMO.Clients
{
    public class ClientLoginModule : ClientModule
    {
        [SerializeField]
        public AuthenticationServiceType _authenticationService;
        [SerializeField]
        public BackendType _backend;

        public event Action<InitializedDataResponseMessage> OnRecievedLoginData;

        public override void Initialize()
        {
            UDPClient.RegisterHandler<RegistrationResponseMessage>(OnRegistrationResponse, false);
            UDPClient.RegisterHandler<ServerLoginReponseMessage>(OnLoginResponse, false);
            UDPClient.RegisterHandler<InitializedDataResponseMessage>(OnDataResponse);
        }        

        public void RegisterAccount()
        {
            var msg = new RegistrationRequestMessage()
            {
                AccountName = "Dev Account",
                ResponseID = UDPClient.RegisterResponse<RegistrationRequestMessage>(10),
            };

            UDPClient.Send(msg);
        }

        public void Login()
        {
            var msg = new AuthenticationMessage()
            {
                accountName = "Dev Account",
                ResponseID = UDPClient.RegisterResponse<AuthenticationMessage>(10)
            };

            UDPClient.Send(msg);
        }


        private void OnRegistrationResponse(INetConnection conn, RegistrationResponseMessage msg)
        {
            if (NetLogFilter.logInfo)
            {
                Debug.Log($"Registration Response: {msg.Status}");
            }

            var success = UDPClient.ServerPeer.TriggerResponse(msg.ResponseID, msg.Status);
            if (msg.Status == ResponseStatus.Timeout)
            {
                UDPClient.ServerPeer.TriggerResponse(msg.ResponseID, msg.Status);
            }
            else
            {
                if (success)
                {
                    if (msg.Status == ResponseStatus.Success)
                    {
                        Login();
                    }
                }
            }
        }

        private void OnLoginResponse(INetConnection conn, ServerLoginReponseMessage msg)
        {
            if (NetLogFilter.logInfo)
            {
                Debug.Log($"Login Response: {msg.Status}");
            }

            var success = UDPClient.ServerPeer.TriggerResponse(msg.ResponseID, msg.Status);
            if (msg.Status == ResponseStatus.Timeout)
            {
                
            }
            else
            {
                if (success)
                {
                    if (msg.Status == ResponseStatus.Success)
                    {
                        conn.Authenticated();
                        var requestData = new InitializationRequestMessage()
                        {
                            ResponseID = UDPClient.RegisterResponse<InitializationRequestMessage>(10)
                        };

                        UDPClient.Send(requestData);
                    }
                }
            }
        }

        private void OnDataResponse(INetConnection conn, InitializedDataResponseMessage msg)
        {
            if (NetLogFilter.logInfo)
            {
                Debug.Log($"Login Data Response: {msg.Status}");
            }

            var success = UDPClient.ServerPeer.TriggerResponse(msg.ResponseID, msg.Status);
            if (msg.Status == ResponseStatus.Timeout)
            {
                UDPClient.ServerPeer.TriggerResponse(msg.ResponseID, msg.Status);
            }
            else
            {
                if (success)
                {
                    if (msg.Status == ResponseStatus.Success)
                    {
                        if (NetLogFilter.logInfo)
                        {
                            Debug.Log($"Login Data Result: {msg.result}");
                        }
                        OnRecievedLoginData?.Invoke(msg);
                    }
                }
            }
        }
    }
}
