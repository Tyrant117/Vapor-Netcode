using System.Collections.Generic;
using VaporNetcode;

namespace VaporMMO
{
    public struct InterestMessage : INetMessage
    {
        public List<EntityInterestPacket> packets;

        public InterestMessage(NetworkReader r)
        {
            packets = new();
            int count = r.ReadInt();
            for (int i = 0; i < count; i++)
            {
                packets.Add(new EntityInterestPacket(r));
            }
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteInt(packets.Count);
            for (int i = 0; i < packets.Count; i++)
            {
                packets[i].Serialize(w);
            }
        }
    }
}
