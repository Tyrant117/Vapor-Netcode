using VaporNetcode;

namespace VaporMMO
{
    public struct JoinWithCharacterRequestMessage : INetMessage, IResponsePacket
    {
        public string CharacterName;

        public ushort ResponseID { get; set; }
        public ResponseStatus Status { get; set; }

        public JoinWithCharacterRequestMessage(NetworkReader r)
        {
            CharacterName = r.ReadString();

            ResponseID = r.ReadUShort();
            Status = (ResponseStatus)r.ReadByte();
        }


        public void Serialize(NetworkWriter w)
        {
            w.WriteString(CharacterName);

            w.WriteUShort(ResponseID);
            w.WriteByte((byte)Status);
        }
    }
}
