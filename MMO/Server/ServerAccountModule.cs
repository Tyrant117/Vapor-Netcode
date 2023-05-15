using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using VaporMMO.Backend;
using VaporNetcode;

namespace VaporMMO.Servers
{
    public class ServerAccountModule : ServerModule
    {
        private const string TAG = "<color=yellow><b>[Server Account]</b></color>";

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
            var rsa = RSA.Create(2048);

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

        // Database
        private Action<INetConnection, GetAccountDataRequestMessage,
            AccountSpecificationLookup, // Account Lookup
            AccountCharacterLookup, // Character Lookup
            AccountDataLookup> AccountLookup; // Characters Returned


        public override void Initialize()
        {
            UDPServer.RegisterHandler<RegistrationRequestMessage>(OnHandleRegistration, false);
            UDPServer.RegisterHandler<LoginRequestMessage>(OnHandleLogin, false);
            UDPServer.RegisterHandler<GetAccountDataRequestMessage>(OnHandleGetAccountData);
            UDPServer.RegisterHandler<CreateCharacterRequestMessage>(OnHandleCreateCharacter);
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
            if (NetLogFilter.logInfo) { Debug.Log($"{TAG} Registering New User: {msg.AccountName}"); }
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
            if (accounts.TryGetValue(accountID, out var accSpec))
            {
                if (NetLogFilter.logInfo) { Debug.Log($"{TAG} User Already Exhists: {accountID}"); }
                RegistrationResponseMessage respPacket = new()
                {
                    AccountName = accountID,
                    Password = msg.Password,
                    Status = ResponseStatus.Failed
                };
                UDPServer.Respond(conn, respPacket);
            }
            else
            {
                var decrypt = Decrypt(msg.Password);
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
        }        
        #endregion

        #region - Login -
        private void OnHandleLogin(INetConnection conn, LoginRequestMessage msg)
        {
            if (NetLogFilter.logInfo) { Debug.Log($"{TAG} Login: {msg.accountName} | {conn.IsAuthenticated}"); }
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
                    conn.Authenticated(conn.ConnectionID);
                    conn.GenericStringID = msg.accountName;
                    OnAuthenticatedResult(conn, msg);
                }
                else
                {
                    if (NetLogFilter.logWarn)
                    {
                        Debug.Log($"{TAG} Unable to verify password: {password}");
                    }
                }
            }
        }

        private void OnAuthenticatedResult(INetConnection conn, LoginRequestMessage msg)
        {
            if (NetLogFilter.logInfo) { Debug.Log($"{TAG} Authenticated Result: {msg.accountName} | {conn.IsAuthenticated}"); }
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

        private void OnHandleGetAccountData(INetConnection conn, GetAccountDataRequestMessage msg)
        {
            if (NetLogFilter.logInfo) { Debug.Log($"{TAG} Get Account Data"); }
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
            if (!account.HasValue)
            {
                GetAccountDataResponseMessage respPacket = new()
                {
                    result = GetAccountDataResponseMessage.InitializationResult.NoAccountFound,
                    Status = ResponseStatus.Success,
                    characters = new(),
                };
                if (NetLogFilter.logWarn) { Debug.Log($"{TAG} Account Data Not Found"); }
                UDPServer.Respond(conn, respPacket);
            }
            else
            {
                if (NetLogFilter.logInfo) { Debug.Log($"{TAG} Account Data Found: {account.Value.StringID}"); }
                accounts[conn.GenericStringID] = account.Value;
                characters[conn.GenericStringID] = new();
            }
        }

        private void OnCharacterCountResult(INetConnection conn, GetAccountDataRequestMessage msg, int characterCount)
        {
            if (NetLogFilter.logInfo) { Debug.Log($"{TAG} Character Count: {conn.GenericStringID} : {characterCount}"); }
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
                if (NetLogFilter.logInfo) { Debug.Log($"{TAG} Character Data: {conn.GenericStringID} : {respPacket.characters.Count}"); }
                UDPServer.Respond(conn, respPacket);
            }
        }
        #endregion

        #region - Joining -
        private void OnHandleCreateCharacter(INetConnection conn, CreateCharacterRequestMessage msg)
        {
            var response = new CreateCharacterResponseMessage()
            {
                Status = ResponseStatus.Failed
            };
            if (characters.TryGetValue(conn.GenericStringID, out var chars) && chars.Count < 14)
            {
                if (!UDPServer.GetModule<ServerWorldModule>().TryCreateNewCharacter(conn.GenericStringID, msg.CharacterName, msg, out var character))
                {
                    response = new CreateCharacterResponseMessage()
                    {
                        CharacterName = msg.CharacterName,
                        Status = ResponseStatus.Failed
                    };
                }
                else
                {
                    chars.Add(character);
                    if (NetLogFilter.logInfo) { Debug.Log($"{TAG} Character Created: {conn.GenericStringID} : {character.CharacterName}"); }
                    response = new CreateCharacterResponseMessage()
                    {
                        CharacterName = msg.CharacterName,
                        Status = ResponseStatus.Success
                    };
                }
            }
            UDPServer.Send(conn, response);
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
                        if (NetLogFilter.logInfo) { Debug.Log($"{TAG} Joined With Character: {conn.GenericStringID} : {msg.CharacterName}"); }
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
            AccountSpecifcation? foundAccount = null;
            switch (_backend)
            {
                case BackendType.Local:
                    _FromLocal();
                    break;
                case BackendType.Unisave:
                    break;
                case BackendType.Unity:
                    break;
                case BackendType.Playfab:
                    break;
            }
            return foundAccount;

            void _FromLocal()
            {
                string localDataPath = Application.persistentDataPath + "/Accounts.json";
                if (System.IO.File.Exists(localDataPath))
                {
                    string jsonFile = System.IO.File.ReadAllText(localDataPath);
                    var db = JsonUtility.FromJson<AccountSpecJSON>(jsonFile);

                    foreach (var account in db.Specifications)
                    {
                        if (accountName == account.StringID)
                        {
                            foundAccount = account;
                            break;
                        }
                    }
                }
            }
        }

        private void HandleAccountLookupLocal(INetConnection conn, GetAccountDataRequestMessage msg, AccountSpecificationLookup lookup, AccountCharacterLookup characterCount, AccountDataLookup characterCallback)
        {
            if (NetLogFilter.logInfo) { Debug.Log($"{TAG} Handle Account Lookup Local"); }
            string localDataPath = Application.persistentDataPath + "/Accounts.json";
            string localCharacterPath = Application.persistentDataPath + "/Characters.json";
            var characters = new List<AccountDataSpecification>();

            AccountSpecifcation? foundAccount = null;
            if (System.IO.File.Exists(localDataPath))
            {
                string jsonFile = System.IO.File.ReadAllText(localDataPath);
                var db = JsonUtility.FromJson<AccountSpecJSON>(jsonFile);

                foreach (var account in db.Specifications)
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

                    foreach (var character in db.Specifications)
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
            public List<AccountSpecifcation> Specifications;

            public AccountSpecJSON(AccountSpecifcation[] specifications)
            {
                Specifications = new(specifications);
            }
        }

        [Serializable]
        private class AccountDataSpecJSON
        {
            public List<AccountDataSpecification> Specifications;

            public AccountDataSpecJSON()
            {
                Specifications = new();
            }

            public void AddRange(List<AccountDataSpecification> specifications)
            {
                Specifications.AddRange(specifications);
            }
        }
        #endregion

        #region - Social -

        #endregion

        #region - Helpers -
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
    }
}
