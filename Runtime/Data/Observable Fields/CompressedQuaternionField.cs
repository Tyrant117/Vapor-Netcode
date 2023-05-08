using System;
using UnityEngine;

namespace VaporNetcode
{
    public class CompressedQuaternionField : ObservableField
    {
        public static implicit operator Quaternion(CompressedQuaternionField f) => f.Value;

        public Quaternion Value { get; protected set; }
        public event Action<CompressedQuaternionField> ValueChanged;

        public CompressedQuaternionField(ObservableClass @class, int fieldID, bool saveValue, Quaternion value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.CompressedQuaternion;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public CompressedQuaternionField(int fieldID, bool saveValue, bool isServer, Quaternion value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.CompressedQuaternion;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetQuaternion(Quaternion value)
        {
            if (Type == ObservableFieldType.CompressedQuaternion)
            {
                if (Value != value)
                {
                    Value = value;
                    ValueChanged?.Invoke(this);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void ExternalSet(Quaternion value)
        {
            if (SetQuaternion(value))
            {
                if (IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }
        #endregion

        #region - Serialization -
        public override bool Serialize(NetworkWriter w)
        {
            if (base.Serialize(w))
            {
                w.WriteUInt(Compression.CompressQuaternion(Value));
                IsServerDirty = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool SerializeInFull(NetworkWriter w)
        {
            return Serialize(w);
        }

        public override bool Deserialize(NetworkReader r)
        {
            return base.Deserialize(r) && SetQuaternion(Compression.DecompressQuaternion(r.ReadUInt()));
        }
        #endregion

        #region - Saving -
        public override SavedObservable Save()
        {
            return new SavedObservable(FieldID, Type, $"{Value.x},{Value.y},{Value.z},{Value.w}");
        }
        #endregion
    }
}
