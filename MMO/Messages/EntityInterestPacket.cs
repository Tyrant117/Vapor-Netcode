using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporNetcode;

namespace VaporMMO
{
    public struct EntityInterestPacket : ISerializablePacket
    {
        public byte InterestType;
        public uint NetID;
        public ArraySegment<byte> Data;

        public EntityInterestPacket(NetworkReader r)
        {
            InterestType = r.ReadByte();
            NetID = r.ReadUInt();
            Data = r.ReadBytesAndSizeSegment();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteByte(InterestType);
            w.WriteUInt(NetID);
            w.WriteBytesAndSizeSegment(Data);
        }

        public int AsConnectionID()
        {
            return Convert.ToInt32(NetID);
        }
    }
}
