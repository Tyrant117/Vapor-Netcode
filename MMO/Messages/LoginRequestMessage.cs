using VaporNetcode;

namespace VaporMMO
{
    public struct LoginRequestMessage : INetMessage
    {
        public AuthenticationServiceType authenticationService;
        public string accountName;
        public byte[] Password;
        public PlayFabAuthenticationPacket playFabAuthentication;

        public LoginRequestMessage(NetworkReader reader)
        {
            authenticationService = (AuthenticationServiceType)reader.ReadByte();
            playFabAuthentication = default;
            accountName = string.Empty;            
            switch (authenticationService)
            {
                case AuthenticationServiceType.None:
                    accountName = reader.ReadString();
                    break;
                case AuthenticationServiceType.Unity:
                    break;
                case AuthenticationServiceType.Playfab:
                    playFabAuthentication = new PlayFabAuthenticationPacket()
                    {
                        playfabId = reader.ReadString(),
                        sessionToken = reader.ReadString()
                    };
                    break;
                case AuthenticationServiceType.Steam:
                    break;
                case AuthenticationServiceType.Epic:
                    break;
                case AuthenticationServiceType.Custom:
                    break;
            }
            Password = reader.ReadBytesAndSize();
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteByte((byte)authenticationService);
            switch (authenticationService)
            {
                case AuthenticationServiceType.None:
                    writer.WriteString(accountName);
                    break;
                case AuthenticationServiceType.Unity:
                    break;
                case AuthenticationServiceType.Playfab:
                    playFabAuthentication.Write(writer);
                    break;
                case AuthenticationServiceType.Steam:
                    break;
                case AuthenticationServiceType.Epic:
                    break;
                case AuthenticationServiceType.Custom:
                    break;
            }
            writer.WriteBytesAndSize(Password);
        }
    }

    public struct LoginReponseMessage : IResponseMessage
    {
        public AuthenticationServiceType AuthenticationService;
        public int ConnectionID;
        public ulong SteamID;
        public string StringID;

        public ResponseStatus Status { get; set; }

        public LoginReponseMessage(NetworkReader r)
        {
            AuthenticationService = (AuthenticationServiceType)r.ReadByte();
            ConnectionID = r.ReadInt();
            SteamID = 0;
            StringID = string.Empty;
            switch (AuthenticationService)
            {
                case AuthenticationServiceType.None:
                    StringID = r.ReadString();
                    break;
                case AuthenticationServiceType.Unity:
                    StringID = r.ReadString();
                    break;
                case AuthenticationServiceType.Playfab:
                    StringID = r.ReadString();
                    break;
                case AuthenticationServiceType.Steam:
                    SteamID = Compression.DecompressVarUInt(r);
                    break;
                case AuthenticationServiceType.Epic:
                    StringID = r.ReadString();
                    break;
                case AuthenticationServiceType.Custom:
                    StringID = r.ReadString();
                    break;
            }

            Status = (ResponseStatus)r.ReadByte();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteByte((byte)AuthenticationService);
            w.WriteInt(ConnectionID);
            switch (AuthenticationService)
            {
                case AuthenticationServiceType.None:
                    w.WriteString(StringID);
                    break;
                case AuthenticationServiceType.Unity:
                    w.WriteString(StringID);
                    break;
                case AuthenticationServiceType.Playfab:
                    w.WriteString(StringID);
                    break;
                case AuthenticationServiceType.Steam:
                    Compression.CompressVarUInt(w, SteamID);
                    break;
                case AuthenticationServiceType.Epic:
                    w.WriteString(StringID);
                    break;
                case AuthenticationServiceType.Custom:
                    w.WriteString(StringID);
                    break;
            }

            w.WriteByte((byte)Status);
        }
    }
}
