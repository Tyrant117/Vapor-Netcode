using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporMMO.Backend;
using VaporNetcode;
using System.Security.Cryptography;
using System.Text;

namespace VaporMMO.Clients
{
    public class ClientLoginModule : ClientModule
    {
        private const string TAG = "<color=orange><b>[Client Login]</b></color>";

        [SerializeField]
        public AuthenticationServiceType _authenticationService;
        [SerializeField]
        public BackendType _backend;
        [SerializeField]
        public string _rsaPublicKey;

        public event Action<GetAccountDataResponseMessage> OnRecievedLoginData;

        public override void Initialize()
        {
            UDPClient.RegisterHandler<RegistrationResponseMessage>(OnRegistrationResponse, false);
            UDPClient.RegisterHandler<LoginReponseMessage>(OnLoginResponse, false);
            UDPClient.RegisterHandler<GetAccountDataResponseMessage>(OnGetAccountDataResponse);
            UDPClient.RegisterHandler<CreateCharacterResponseMessage>(OnCharacterCreateResponse);
        }

        public void RegisterAccount(string accountName, string password)
        {
            var rsa = RSA.Create();
            rsa.FromXmlString(_rsaPublicKey);
            var encrypted = rsa.Encrypt(Encoding.UTF8.GetBytes(password), RSAEncryptionPadding.Pkcs1);
            var msg = new RegistrationRequestMessage()
            {
                AccountName = accountName,
                Password = encrypted,
            };

            UDPClient.RegisterResponse<RegistrationResponseMessage>(10);
            UDPClient.Send(msg);
        }

        public void Login(string accountName, string password)
        {
            var rsa = RSA.Create();
            rsa.FromXmlString(_rsaPublicKey);
            var encrypted = rsa.Encrypt(Encoding.UTF8.GetBytes(password), RSAEncryptionPadding.Pkcs1);
            var msg = new LoginRequestMessage()
            {
                accountName = accountName,
                Password = encrypted,
            };
            UDPClient.RegisterResponse<LoginReponseMessage>(10);
            UDPClient.Send(msg);
        }

        private void Login(string accountName, byte[] password)
        {
            var msg = new LoginRequestMessage()
            {
                accountName = accountName,
                Password = password,
            };
            UDPClient.RegisterResponse<LoginReponseMessage>(10);
            UDPClient.Send(msg);
        }

        public void Create(string characterName, ArraySegment<byte> data)
        {
            var msg = new CreateCharacterRequestMessage()
            {
                CharacterName = characterName,
                CreationPacket = data,
            };
            UDPClient.RegisterResponse<CreateCharacterResponseMessage>(10);
            UDPClient.Send(msg);
        }

        public void Join(string characterName)
        {
            var msg = new JoinWithCharacterRequestMessage()
            {
                CharacterName = characterName
            };
            UDPClient.RegisterResponse<JoinWithCharacterResponseMessage>(10);
            UDPClient.Send(msg);
        }


        private void OnRegistrationResponse(INetConnection conn, RegistrationResponseMessage msg)
        {
            if (NetLogFilter.logInfo)
            {
                Debug.Log($"{TAG} Registration Response: {msg.Status}");
            }

            var success = UDPClient.ServerPeer.TriggerResponse<RegistrationResponseMessage>(msg.Status);
            if (success)
            {
                if (msg.Status == ResponseStatus.Success)
                {

                    Login(msg.AccountName, msg.Password);
                }
            }
        }

        private void OnLoginResponse(INetConnection conn, LoginReponseMessage msg)
        {
            if (NetLogFilter.logInfo)
            {
                Debug.Log($"{TAG} Login Response: {msg.Status}");
            }

            var success = UDPClient.ServerPeer.TriggerResponse<LoginReponseMessage>(msg.Status);
            if (success)
            {
                if (msg.Status == ResponseStatus.Success)
                {
                    conn.Authenticated(msg.ConnectionID);
                    var requestData = new GetAccountDataRequestMessage()
                    {

                    };
                    UDPClient.RegisterResponse<GetAccountDataResponseMessage>(10);
                    UDPClient.Send(requestData);
                }
            }
        }

        private void OnGetAccountDataResponse(INetConnection conn, GetAccountDataResponseMessage msg)
        {
            if (NetLogFilter.logInfo)
            {
                Debug.Log($"{TAG} Login Data Response: {msg.Status}");
            }

            var success = UDPClient.ServerPeer.TriggerResponse<GetAccountDataResponseMessage>(msg.Status);
            if (success)
            {
                if (msg.Status == ResponseStatus.Success)
                {
                    if (NetLogFilter.logInfo)
                    {
                        Debug.Log($"{TAG} Login Data Result: {msg.result}");
                    }
                    OnRecievedLoginData?.Invoke(msg);
                }
            }
        }

        private void OnCharacterCreateResponse(INetConnection conn, CreateCharacterResponseMessage msg)
        {
            if (NetLogFilter.logInfo)
            {
                Debug.Log($"{TAG} Character Create Data Response: {msg.Status}");
            }

            var success = UDPClient.ServerPeer.TriggerResponse<CreateCharacterResponseMessage>(msg.Status);
            if (success)
            {
                if (msg.Status == ResponseStatus.Success)
                {
                    if (NetLogFilter.logInfo)
                    {
                        Debug.Log($"{TAG} Created Character: {msg.CharacterName}");
                    }

                    var join = new JoinWithCharacterRequestMessage()
                    {
                        CharacterName = msg.CharacterName,
                    };
                    UDPClient.RegisterResponse<JoinWithCharacterResponseMessage>(10);
                    UDPClient.Send(join);
                }
            }
        }
    }
}
