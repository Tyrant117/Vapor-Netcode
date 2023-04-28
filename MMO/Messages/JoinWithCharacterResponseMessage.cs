using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporNetcode;

namespace VaporMMO
{
    public struct JoinWithCharacterResponseMessage : INetMessage, IResponsePacket
    {
        public string Scene;
        public Vector3 Position;
        public Quaternion Rotation;

        public ushort ResponseID { get; set; }
        public ResponseStatus Status { get; set; }

        public JoinWithCharacterResponseMessage(NetworkReader r)
        {
            Scene = r.ReadString();
            Position = r.ReadVector3();
            Rotation = r.ReadQuaternion();

            ResponseID = r.ReadUShort();
            Status = (ResponseStatus)r.ReadByte();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteString(Scene);
            w.WriteVector3(Position);
            w.WriteQuaternion(Rotation);

            w.WriteUShort(ResponseID);
            w.WriteByte((byte)Status);
        }
    }
}
