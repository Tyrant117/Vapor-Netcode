using UnityEngine;
using VaporNetcode;

namespace VaporMMO
{
    public struct JoinWithCharacterRequestMessage : INetMessage
    {
        public string CharacterName;

        public JoinWithCharacterRequestMessage(NetworkReader r)
        {
            CharacterName = r.ReadString();
        }


        public void Serialize(NetworkWriter w)
        {
            w.WriteString(CharacterName);
        }
    }

    public struct JoinWithCharacterResponseMessage : IResponseMessage
    {
        public string Scene;
        public Vector3 Position;
        public Quaternion Rotation;

        public ResponseStatus Status { get; set; }

        public JoinWithCharacterResponseMessage(NetworkReader r)
        {
            Scene = r.ReadString();
            Position = r.ReadVector3();
            Rotation = r.ReadQuaternion();

            Status = (ResponseStatus)r.ReadByte();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteString(Scene);
            w.WriteVector3(Position);
            w.WriteQuaternion(Rotation);

            w.WriteByte((byte)Status);
        }
    }
}
