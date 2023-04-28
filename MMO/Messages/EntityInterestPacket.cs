using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporNetcode;

namespace VaporMMO
{
    public struct EntityInterestPacket : ISerializablePacket
    {
        public int ConnectionID;
        public byte InterestType;
        public ArraySegment<byte> Data;

        public EntityInterestPacket(NetworkReader r)
        {
            ConnectionID = r.ReadInt();
            InterestType = r.ReadByte();
            Data = r.ReadBytesAndSizeSegment();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteInt(ConnectionID);
            w.WriteByte(InterestType);
            w.WriteBytesAndSizeSegment(Data);
        }
    }
}
