using VaporNetcode;

namespace VaporMMO
{
    public struct AuthenticationMessage : INetMessage, IResponsePacket
    {
        public AuthenticationServiceType authenticationService;
        public string accountName;
        public PlayFabAuthenticationPacket playFabAuthentication;

        public ushort ResponseID { get; set; }
        public ResponseStatus Status { get; set; }

        public AuthenticationMessage(NetworkReader reader)
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

            ResponseID = reader.ReadUShort();
            Status = (ResponseStatus)reader.ReadByte();
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

            writer.WriteUShort(ResponseID);
            writer.WriteByte((byte)Status);
        }
    }
}
