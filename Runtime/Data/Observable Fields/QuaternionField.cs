using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public class QuaternionField : ObservableField
    {
        public static implicit operator Quaternion(QuaternionField f) => f.Value;

        public Quaternion Value { get; protected set; }
        public event Action<QuaternionField> ValueChanged;

        public QuaternionField(ObservableClass @class, int fieldID, bool saveValue, Quaternion value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Quaternion;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public QuaternionField(int fieldID, bool saveValue, bool isNetworkSynced, bool isServer, Quaternion value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.Quaternion;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetQuaternion(Quaternion value)
        {
            if (Type == ObservableFieldType.Quaternion)
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
                w.WriteQuaternion(Value);
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
            return base.Deserialize(r) && SetQuaternion(r.ReadQuaternion());
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
