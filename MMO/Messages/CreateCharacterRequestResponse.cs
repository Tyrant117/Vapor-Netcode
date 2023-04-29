using VaporNetcode;

namespace VaporMMO
{
    public struct CreateCharacterRequestMessage : INetMessage
    {

        public void Serialize(NetworkWriter w)
        {
        }
    }

    public struct CreateCharacterResponseMessage : IResponseMessage
    {
        public string CharacterName;
        public ResponseStatus Status { get; set; }

        public CreateCharacterResponseMessage(NetworkReader r)
        {
            CharacterName = r.ReadString();
            Status = (ResponseStatus)r.ReadByte();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteString(CharacterName);
            w.WriteByte((byte)Status);
        }
    }
}
