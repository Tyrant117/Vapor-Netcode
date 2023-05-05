using System.Collections.Generic;
using System;
using VaporNetcode;
using UnityEngine;
using System.Linq;
using VaporMMO.Backend;
using Sirenix.OdinInspector;
using System.Text;
using System.Security.Cryptography;

namespace VaporMMO.Servers
{
    public class ServerAccountModule : ServerModule
    {
        private delegate void AccountSpecificationLookup(INetConnection conn, GetAccountDataRequestMessage msg, AccountSpecifcation? account);
        private delegate void AccountCharacterLookup(INetConnection conn, GetAccountDataRequestMessage msg, int characterCount);
        private delegate void AccountDataLookup(INetConnection conn, GetAccountDataRequestMessage msg, AccountDataSpecification? accountData);        

        // Authenticate with external service, unity or playfab.
        // Request ServerInfo to connect to, either this will be known because its hosted or youll get it from playfab.
        // Connect to Server with an AccountModule. AccountModule should listen for an backend Authentication request.
        // Once the account is logged in, the player can then ask for character data.
        [SerializeField]
        public AuthenticationServiceType _authenticationService;
        [SerializeField]
        public BackendType _backend;

        [SerializeField]
        public string _publicRSAKey;
        [SerializeField]
        public string _privateRSAKey;

        [Button]
        private void GenerateRSAKeys()
        {
            var rsa = System.Security.Cryptography.RSA.Create(2048);

            //how to get the private key
            _privateRSAKey = rsa.ToXmlString(true);

            //and the public key ...
            _publicRSAKey = rsa.ToXmlString(false);
        }

        private readonly Dictionary<string, AccountSpecifcation> accounts = new();
        private readonly Dictionary<string, List<AccountDataSpecification>> characters = new();
        private readonly Dictionary<string, Peer> connectedPeers = new();
        private readonly Dictionary<string, int> pendingCharacterDataCount = new();

        // Authentication
        private Action<INetConnection, LoginRequestMessage> AuthenticateConnection;

        public Action<INetConnection, CreateCharacterRequestMessage> CreateCharacter;

        // Database
        private Action<INetConnection, GetAccountDataRequestMessage,
            AccountSpecificationLookup, // Account Lookup
            AccountCharacterLookup, // Character Lookup
            AccountDataLookup> AccountLookup; // Characters Returned


        public override void Initialize()
        {
            UDPServer.RegisterHandler<RegistrationRequestMessage>(OnHandleRegistration, false);
            UDPServer.RegisterHandler<LoginRequestMessage>(OnHandleLogin, false);
            UDPServer.RegisterHandler<GetAccountDataRequestMessage>(OnHandleInitializationData);
            UDPServer.RegisterHandler<JoinWithCharacterRequestMessage>(OnHandleJoinWithCharacter);

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

            var decrypt = Decrypt(msg.Password);
            Debug.Log(decrypt);
            var password = Hash(decrypt, out var salt);

            AccountSpecifcation account = new()
            {
                StringID = accountID,
                LinkedSteamID = msg.SteamID,
                LinkedEpicProductUserID = msg.EpicUserID,
                Email = msg.Email,
                Password = password,
                Salt = salt,
                Permissions = PermisisonLevel.None,
                EndOfBan = DateTimeOffset.MinValue,
                LastLoggedIn = DateTimeOffset.UtcNow,
            };

            accounts[account.StringID] = account;

            WriteToDatabase();

            RegistrationResponseMessage respPacket = new()
            {
                AccountName = accountID,
                Password = msg.Password,
                Status = ResponseStatus.Success
            };

            UDPServer.Respond(conn, respPacket);
        }

        private string Decrypt(byte[] bytes)
        {
            var rsa = RSA.Create();
            rsa.FromXmlString(_privateRSAKey);
            return Encoding.UTF8.GetString(rsa.Decrypt(bytes, RSAEncryptionPadding.Pkcs1));
        }

        private string Hash(string password, out string salt)
        {
            var rng = RandomNumberGenerator.Create();
            var bytes = new byte[64];
            rng.GetBytes(bytes);
            var hash = new Rfc2898DeriveBytes(password, bytes, 10000, HashAlgorithmName.SHA512);
            salt = BitConverter.ToString(hash.Salt).Replace("-", string.Empty);
            return BitConverter.ToString(hash.GetBytes(64)).Replace("-", string.Empty);
        }

        private bool VerifyPassword(string password, string hash, string salt)
        {
            var bytes = StringToByteArray(salt);
            var hashToCompare = new Rfc2898DeriveBytes(password, bytes, 10000, HashAlgorithmName.SHA512);
            return hashToCompare.GetBytes(64).SequenceEqual(StringToByteArray(hash));
        }

        public static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }
        #endregion

        #region - Login -
        private void OnHandleLogin(INetConnection conn, LoginRequestMessage msg)
        {
            if (!conn.IsAuthenticated)
            {
                AuthenticateConnection.Invoke(conn, msg);
            }
        }

        private void HandleAuthenticationNone(INetConnection conn, LoginRequestMessage msg)
        {
            var account = GetAccount(msg.accountName);
            if (account.HasValue)
            {
                var password = Decrypt(msg.Password);
                if (VerifyPassword(password, account.Value.Password, account.Value.Salt))
                {
                    conn.Authenticated();
                    conn.GenericStringID = msg.accountName;
                    OnAuthenticatedResult(conn, msg);
                }
                else
                {
                    Debug.Log($"Unable to verify password: {password}");
                }
            }
        }

        private void OnAuthenticatedResult(INetConnection conn, LoginRequestMessage msg)
        {
            var respPacket = new LoginReponseMessage()
            {
                AuthenticationService = _authenticationService,
                ConnectionID = 0,
                Status = ResponseStatus.Failed
            };

            if (conn.IsAuthenticated)
            {
                connectedPeers[conn.GenericStringID] = (Peer)conn;
                // Need to respond to the login success with the list of characters and names for the player.
                respPacket = new LoginReponseMessage()
                {
                    AuthenticationService = _authenticationService,
                    ConnectionID = conn.ConnectionID,
                    Status = ResponseStatus.Success
                };

                switch (_authenticationService)
                {
                    case AuthenticationServiceType.None:
                        respPacket.StringID = conn.GenericStringID;
                        break;
                    case AuthenticationServiceType.Unity:
                        respPacket.StringID = conn.GenericStringID;
                        break;
                    case AuthenticationServiceType.Playfab:
                        respPacket.StringID = conn.GenericStringID;
                        break;
                    case AuthenticationServiceType.Steam:
                        respPacket.SteamID = conn.GenericULongID;
                        break;
                    case AuthenticationServiceType.Epic:
                        respPacket.StringID = conn.GenericStringID;
                        break;
                    case AuthenticationServiceType.Custom:
                        respPacket.StringID = conn.GenericStringID;
                        break;
                }
            }
            UDPServer.Respond(conn, respPacket);
        }

        private void OnHandleInitializationData(INetConnection conn, GetAccountDataRequestMessage msg)
        {
            if (conn.IsAuthenticated)
            {
                AccountLookup?.Invoke(conn, msg, OnAccountDataResult, OnCharacterCountResult, OnCharacterDataResult);
            }
            else
            {
                UDPServer.FlagPossibleSpam(conn);
            }
        }

        private void OnAccountDataResult(INetConnection conn, GetAccountDataRequestMessage msg, AccountSpecifcation? account)
        {
            if (account == null)
            {
                GetAccountDataResponseMessage respPacket = new()
                {
                    result = GetAccountDataResponseMessage.InitializationResult.NoAccountFound,
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

        private void OnCharacterCountResult(INetConnection conn, GetAccountDataRequestMessage msg, int characterCount)
        {
            if (characterCount == 0)
            {
                GetAccountDataResponseMessage respPacket = new()
                {
                    result = GetAccountDataResponseMessage.InitializationResult.NoCharactersFound,
                    permissions = accounts[conn.GenericStringID].Permissions,
                    endOfBan = accounts[conn.GenericStringID].EndOfBan ?? default,
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

        private void OnCharacterDataResult(INetConnection conn, GetAccountDataRequestMessage msg, AccountDataSpecification? character)
        {
            if (character != null)
            {
                characters[conn.GenericStringID].Add((AccountDataSpecification)character);
            }
            int c = pendingCharacterDataCount[conn.GenericStringID] - 1;
            pendingCharacterDataCount[conn.GenericStringID] = c;

            if (c == 0)
            {
                GetAccountDataResponseMessage respPacket = new()
                {
                    result = GetAccountDataResponseMessage.InitializationResult.AccountWithCharactersFound,
                    permissions = accounts[conn.GenericStringID].Permissions,
                    endOfBan = (DateTimeOffset)(accounts[conn.GenericStringID].EndOfBan != null ? accounts[conn.GenericStringID].EndOfBan : default),
                    Status = ResponseStatus.Success,
                    characters = new(),
                };
                respPacket.FormatAccountData(characters[conn.GenericStringID]);
                UDPServer.Respond(conn, respPacket);
            }
        }

        private void OnHandleJoinWithCharacter(INetConnection conn, JoinWithCharacterRequestMessage msg)
        {
            if (characters.TryGetValue(conn.GenericStringID, out var c))
            {
                bool found = false;
                foreach (var character in c)
                {
                    if (character.CharacterName == msg.CharacterName)
                    {
                        found = true;
                        var response = UDPServer.GetModule<ServerWorldModule>().Join(conn, character);
                        UDPServer.Send(conn, response);
                        break;
                    }
                }

                if (!found)
                {
                    conn.Disconnect();
                }
            }
        }
        #endregion

        #region - Database -
        private AccountSpecifcation? GetAccount(string accountName)
        {
            string localDataPath = Application.persistentDataPath + "/Accounts.json";
            AccountSpecifcation? foundAccount = null;
            if (System.IO.File.Exists(localDataPath))
            {
                string jsonFile = System.IO.File.ReadAllText(localDataPath);
                var db = JsonUtility.FromJson<AccountSpecJSON>(jsonFile);

                foreach (var account in db.Specifcations)
                {
                    if (accountName == account.StringID)
                    {
                        foundAccount = account;
                        break;
                    }
                }
            }
            return foundAccount;
        }

        private void HandleAccountLookupLocal(INetConnection conn, GetAccountDataRequestMessage msg, AccountSpecificationLookup lookup, AccountCharacterLookup characterCount, AccountDataLookup characterCallback)
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
