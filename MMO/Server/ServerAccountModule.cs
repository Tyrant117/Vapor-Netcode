using System.Collections.Generic;
using System;
using VaporNetcode;
using UnityEngine;
using System.Linq;
using VaporMMO.Backend;

namespace VaporMMO.Servers
{
    public class ServerAccountModule : ServerModule
    {
        private delegate void AccountSpecificationLookup(INetConnection conn, InitializationRequestMessage msg, AccountSpecifcation? account);
        private delegate void AccountCharacterLookup(INetConnection conn, InitializationRequestMessage msg, int characterCount);
        private delegate void AccountDataLookup(INetConnection conn, InitializationRequestMessage msg, AccountDataSpecification? accountData);        

        // Authenticate with external service, unity or playfab.
        // Request ServerInfo to connect to, either this will be known because its hosted or youll get it from playfab.
        // Connect to Server with an AccountModule. AccountModule should listen for an backend Authentication request.
        // Once the account is logged in, the player can then ask for character data.
        [SerializeField]
        public AuthenticationServiceType _authenticationService;
        [SerializeField]
        public BackendType _backend;

        private readonly Dictionary<string, AccountSpecifcation> accounts = new();
        private readonly Dictionary<string, List<AccountDataSpecification>> characters = new();
        private readonly Dictionary<string, Peer> connectedPeers = new();
        private readonly Dictionary<string, int> pendingCharacterDataCount = new();

        // Authentication
        private Action<INetConnection, AuthenticationMessage> AuthenticateConnection;

        // Database
        private Action<INetConnection, InitializationRequestMessage,
            AccountSpecificationLookup, // Account Lookup
            AccountCharacterLookup, // Character Lookup
            AccountDataLookup> AccountLookup; // Characters Returned


        public override void Initialize()
        {
            UDPServer.RegisterHandler<RegistrationRequestMessage>(OnHandleRegistration, false);
            UDPServer.RegisterHandler<AuthenticationMessage>(OnHandleLogin, false);
            UDPServer.RegisterHandler<InitializationRequestMessage>(OnHandleInitializationData);

            switch (_authenticationService)
            {
                case AuthenticationServiceType.None:
                    AuthenticateConnection = HandleAuthenticationNone;
                    break;
                case AuthenticationServiceType.Unity:
                    break;
                case AuthenticationServiceType.Playfab:
                    break;
                case AuthenticationServiceType.Steam:
                    break;
                case AuthenticationServiceType.Epic:
                    break;
                case AuthenticationServiceType.Custom:
                    break;
            }

            switch (_backend)
            {
                case BackendType.Local:
                    AccountLookup = HandleAccountLookupLocal;
                    break;
                case BackendType.Unisave:
                    break;
                case BackendType.Unity:
                    break;
                case BackendType.Playfab:
                    break;
            }
        }

        #region - Registration -
        private void OnHandleRegistration(INetConnection conn, RegistrationRequestMessage msg)
        {
            // Create the New Account Data.
            if (NetLogFilter.logInfo) { Debug.LogFormat("Registering New User: {0}", msg.AccountName); }
            string accountID = string.Empty;
            switch (_authenticationService)
            {
                case AuthenticationServiceType.None:
                    accountID = msg.AccountName;
                    break;
                case AuthenticationServiceType.Unity:
                    break;
                case AuthenticationServiceType.Playfab:
                    break;
                case AuthenticationServiceType.Steam:
                    accountID = msg.SteamID.ToString();
                    break;
                case AuthenticationServiceType.Epic:
                    accountID = msg.EpicUserID;
                    break;
                case AuthenticationServiceType.Custom:
                    break;
            }

            AccountSpecifcation account = new()
            {
                StringID = accountID,
                LinkedSteamID = msg.SteamID,
                LinkedEpicProductUserID = msg.EpicUserID,
                Email = msg.Email,
                Password = msg.Password,
                Permissions = PermisisonLevel.None,
                EndOfBan = DateTimeOffset.MinValue,
                LastLoggedIn = DateTimeOffset.UtcNow,
            };

            accounts[account.StringID] = account;

            WriteToDatabase();

            RegistrationResponseMessage respPacket = new()
            {
                ResponseID = msg.ResponseID,
                Status = ResponseStatus.Success
            };

            UDPServer.Respond(conn, respPacket);
        }
        #endregion

        #region - Login -
        private void OnHandleLogin(INetConnection conn, AuthenticationMessage msg)
        {
            if (!conn.IsAuthenticated)
            {
                AuthenticateConnection.Invoke(conn, msg);
            }
        }

        private void HandleAuthenticationNone(INetConnection conn, AuthenticationMessage msg)
        {
            conn.Authenticated();
            conn.GenericStringID = msg.accountName;
            OnAuthenticatedResult(conn, msg);
        }

        private void OnAuthenticatedResult(INetConnection conn, AuthenticationMessage msg)
        {
            var respPacket = new ServerLoginReponseMessage()
            {
                ResponseID = msg.ResponseID,
                Status = ResponseStatus.Failed
            };

            if (conn.IsAuthenticated)
            {
                connectedPeers[conn.GenericStringID] = (Peer)conn;
                // Need to respond to the login success with the list of characters and names for the player.
                respPacket = new ServerLoginReponseMessage()
                {
                    authenticationService = _authenticationService,
                    ResponseID = msg.ResponseID,
                    Status = ResponseStatus.Success
                };

                switch (_authenticationService)
                {
                    case AuthenticationServiceType.None:
                        break;
                    case AuthenticationServiceType.Unity:
                        break;
                    case AuthenticationServiceType.Playfab:
                        respPacket.stringID = conn.GenericStringID;
                        break;
                    case AuthenticationServiceType.Steam:
                        respPacket.steamID = conn.GenericULongID;
                        break;
                    case AuthenticationServiceType.Epic:
                        respPacket.stringID = conn.GenericStringID;
                        break;
                    case AuthenticationServiceType.Custom:
                        break;
                }
            }
            UDPServer.Respond(conn, respPacket);
        }

        private void OnHandleInitializationData(INetConnection conn, InitializationRequestMessage msg)
        {
            if (conn.IsAuthenticated)
            {
                AccountLookup?.Invoke(conn, msg, OnAccoundDataResult, OnCharacterCountResult, OnCharacterDataResult);
            }
            else
            {
                UDPServer.FlagPossibleSpam(conn);
            }
        }

        private void OnAccoundDataResult(INetConnection conn, InitializationRequestMessage msg, AccountSpecifcation? account)
        {
            if (account == null)
            {
                InitializedDataResponseMessage respPacket = new()
                {
                    result = InitializedDataResponseMessage.InitializationResult.NoAccountFound,
                    ResponseID = msg.ResponseID,
                    Status = ResponseStatus.Success,
                    characters = new(),
                };
                UDPServer.Respond(conn, respPacket);
            }
            else
            {
                accounts[conn.GenericStringID] = account.Value;
                characters[conn.GenericStringID] = new();
            }
        }

        private void OnCharacterCountResult(INetConnection conn, InitializationRequestMessage msg, int characterCount)
        {
            if (characterCount == 0)
            {
                InitializedDataResponseMessage respPacket = new()
                {
                    result = InitializedDataResponseMessage.InitializationResult.NoCharactersFound,
                    permissions = accounts[conn.GenericStringID].Permissions,
                    endOfBan = accounts[conn.GenericStringID].EndOfBan ?? default,
                    ResponseID = msg.ResponseID,
                    Status = ResponseStatus.Success,
                    characters = new(),
                };
                UDPServer.Respond(conn, respPacket);
            }
            else
            {
                pendingCharacterDataCount[conn.GenericStringID] = characterCount;
            }
        }

        private void OnCharacterDataResult(INetConnection conn, InitializationRequestMessage msg, AccountDataSpecification? character)
        {
            if (character != null)
            {
                characters[conn.GenericStringID].Add((AccountDataSpecification)character);
            }
            int c = pendingCharacterDataCount[conn.GenericStringID] - 1;
            pendingCharacterDataCount[conn.GenericStringID] = c;

            if (c == 0)
            {
                InitializedDataResponseMessage respPacket = new()
                {
                    result = InitializedDataResponseMessage.InitializationResult.AccountWithCharactersFound,
                    permissions = accounts[conn.GenericStringID].Permissions,
                    endOfBan = (DateTimeOffset)(accounts[conn.GenericStringID].EndOfBan != null ? accounts[conn.GenericStringID].EndOfBan : default),
                    ResponseID = msg.ResponseID,
                    Status = ResponseStatus.Success,
                    characters = new(),
                };
                respPacket.FormatAccountData(characters[conn.GenericStringID]);
                UDPServer.Respond(conn, respPacket);
            }
        }
        #endregion

        #region - Database -
        private void HandleAccountLookupLocal(INetConnection conn, InitializationRequestMessage msg, AccountSpecificationLookup lookup, AccountCharacterLookup characterCount, AccountDataLookup characterCallback)
        {
            string localDataPath = Application.persistentDataPath + "/Accounts.json";
            string localCharacterPath = Application.persistentDataPath + "/Characters.json";
            var characters = new List<AccountDataSpecification>();

            AccountSpecifcation? foundAccount = null;
            if (System.IO.File.Exists(localDataPath))
            {
                string jsonFile = System.IO.File.ReadAllText(localDataPath);
                var db = JsonUtility.FromJson<AccountSpecJSON>(jsonFile);

                foreach (var account in db.Specifcations)
                {
                    if (conn.GenericStringID == account.StringID)
                    {
                        foundAccount = account;
                        break;
                    }
                }
            }

            if (!foundAccount.HasValue)
            {
                lookup.Invoke(conn, msg, foundAccount);
            }
            else
            {
                lookup.Invoke(conn, msg, foundAccount);
                if (System.IO.File.Exists(localCharacterPath))
                {
                    string jsonFile = System.IO.File.ReadAllText(localCharacterPath);
                    var db = JsonUtility.FromJson<AccountDataSpecJSON>(jsonFile);

                    foreach (var character in db.Specifcations)
                    {
                        if (conn.GenericStringID != character.StringID)
                        {
                            continue;
                        }
                        characters.Add(character);
                    }
                }

                characterCount.Invoke(conn, msg, characters.Count);
                if (characters.Count > 0)
                {
                    foreach (var c in characters)
                    {
                        characterCallback.Invoke(conn, msg, c);
                    }
                }
            }
        }

        private void WriteToDatabase()
        {
            switch (_backend)
            {
                case BackendType.Local:
                    _ToLocal();
                    break;
                case BackendType.Unisave:
                    break;
                case BackendType.Unity:
                    break;
                case BackendType.Playfab:
                    break;
            }

            

            void _ToLocal()
            {
                string localDataPath = Application.persistentDataPath + "/Accounts.json";
                var apjson = new AccountSpecJSON(accounts.Values.ToArray());

                var json = JsonUtility.ToJson(apjson, false);
                System.IO.File.WriteAllText(localDataPath, json);

                string localCharacterPath = Application.persistentDataPath + "/Characters.json";
                var adsjson = new AccountDataSpecJSON();
                foreach (var c in characters.Values)
                {
                    adsjson.AddRange(c);
                }

                var jsonC = JsonUtility.ToJson(adsjson, false);
                System.IO.File.WriteAllText(localCharacterPath, jsonC);
            }
        }

        [Serializable]
        private class AccountSpecJSON
        {
            public List<AccountSpecifcation> Specifcations;

            public AccountSpecJSON(AccountSpecifcation[] specifcations)
            {
                Specifcations = new(specifcations);
            }
        }

        [Serializable]
        private class AccountDataSpecJSON
        {
            public List<AccountDataSpecification> Specifcations;

            public AccountDataSpecJSON()
            {
                Specifcations = new();
            }

            public void AddRange(List<AccountDataSpecification> specifcations)
            {
                Specifcations.AddRange(specifcations);
            }
        }
        #endregion

        #region - Social -

        #endregion
    }
}
