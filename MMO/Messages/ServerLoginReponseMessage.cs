using VaporNetcode;

namespace VaporMMO
{
    public struct ServerLoginReponseMessage : INetMessage, IResponseMessage
    {
        public AuthenticationServiceType AuthenticationService;
        public int ConnectionID;
        public ulong SteamID;
        public string StringID;

        public ushort ResponseID { get; set; }
        public ResponseStatus Status { get; set; }

        public ServerLoginReponseMessage(NetworkReader r)
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

            ResponseID = r.ReadUShort();
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

            w.WriteUShort(ResponseID);
            w.WriteByte((byte)Status);
        }
    }
}
