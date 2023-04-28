using VaporNetcode;

namespace VaporMMO
{
    public struct InitializationRequestMessage : INetMessage, IResponsePacket
    {
        public ushort ResponseID { get; set; }
        public ResponseStatus Status { get; set; }

        public InitializationRequestMessage(NetworkReader r)
        {
            ResponseID = r.ReadUShort();
            Status = (ResponseStatus)r.ReadByte();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteUShort(ResponseID);
            w.WriteByte((byte)Status);
        }
    }
}
