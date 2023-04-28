using VaporNetcode;

namespace VaporMMO
{
    public struct ServerLoginReponseMessage : INetMessage, IResponsePacket
    {
        public AuthenticationServiceType authenticationService;
        public ulong steamID;
        public string stringID;

        public ushort ResponseID { get; set; }
        public ResponseStatus Status { get; set; }

        public ServerLoginReponseMessage(NetworkReader reader)
        {
            authenticationService = (AuthenticationServiceType)reader.ReadByte();
            steamID = 0;
            stringID = string.Empty;
            switch (authenticationService)
            {
                case AuthenticationServiceType.Unity:
                    break;
                case AuthenticationServiceType.Playfab:
                    stringID = reader.ReadString();
                    break;
                case AuthenticationServiceType.Steam:
                    steamID = Compression.DecompressVarUInt(reader);
                    break;
                case AuthenticationServiceType.Epic:
                    stringID = reader.ReadString();
                    break;
                case AuthenticationServiceType.Custom:
                    break;
            }

            ResponseID = reader.ReadUShort();
            Status = (ResponseStatus)reader.ReadByte();
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteByte((byte)authenticationService);
            switch (authenticationService)
            {
                case AuthenticationServiceType.None:
                    break;
                case AuthenticationServiceType.Unity:
                    break;
                case AuthenticationServiceType.Playfab:
                    writer.WriteString(stringID);
                    break;
                case AuthenticationServiceType.Steam:
                    Compression.CompressVarUInt(writer, steamID);
                    break;
                case AuthenticationServiceType.Epic:
                    writer.WriteString(stringID);
                    break;
                case AuthenticationServiceType.Custom:
                    break;                
            }

            writer.WriteUShort(ResponseID);
            writer.WriteByte((byte)Status);
        }
    }
}
