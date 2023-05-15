using System;
using VaporNetcode;

namespace VaporMMO
{
    public struct CreateCharacterRequestMessage : INetMessage
    {
        public string CharacterName;
        public ArraySegment<byte> CreationPacket;

        public CreateCharacterRequestMessage(NetworkReader r)
        {
            CharacterName = r.ReadString();
            CreationPacket = r.ReadBytesAndSizeSegment();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteString(CharacterName);
            w.WriteBytesAndSizeSegment(CreationPacket);
        }

        public T GetPacket<T>() where T : struct, ISerializablePacket
        {
            using var r = NetworkReaderPool.Get(CreationPacket);
            return PacketHelper.Deserialize<T>(r);
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
