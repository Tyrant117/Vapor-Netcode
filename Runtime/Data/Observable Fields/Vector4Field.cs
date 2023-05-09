using System;
using UnityEngine;

namespace VaporNetcode
{
    [Serializable]
    public class Vector4Field : ObservableField
    {
        public static implicit operator Vector4(Vector4Field f) => f.Value;

        public Vector4 Value { get; protected set; }
        public event Action<Vector4Field> ValueChanged;

        public Vector4Field(ObservableClass @class, int fieldID, bool saveValue, Vector4 value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector4;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public Vector4Field(int fieldID, bool saveValue, bool isServer, Vector4 value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.Vector4;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetVector4(Vector4 value)
        {
            if (Type == ObservableFieldType.Vector4)
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

        public void ExternalSet(Vector4 value)
        {
            if (SetVector4(value))
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
        public override bool Serialize(NetworkWriter w, bool clearDirtyFlag = true)
        {
            if (base.Serialize(w, clearDirtyFlag))
            {
                w.WriteVector4(Value);
                if (clearDirtyFlag)
                {
                    IsServerDirty = false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool SerializeInFull(NetworkWriter w)
        {
            return Serialize(w, false);
        }

        public override bool Deserialize(NetworkReader r)
        {
            return base.Deserialize(r) && SetVector4(r.ReadVector4());
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