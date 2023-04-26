using System;
using UnityEngine;

namespace VaporNetcode
{
    [Serializable]
    public class Vector2Field : ObservableField
    {
        public static implicit operator Vector2(Vector2Field f) => f.Value;

        public Vector2 Value { get; protected set; }
        public event Action<Vector2Field, Vector2> ValueChanged;

        public Vector2Field(ObservableClass @class, int fieldID, bool saveValue, Vector2 value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector2;
            Value = value;
            if (IsNetworkSynced && IsServer)
            {
                IsServerDirty = true;
            }
        }

        public Vector2Field(int fieldID, bool saveValue, bool isNetworkSynced, bool isServer, Vector2 value) : base(fieldID, saveValue, isNetworkSynced, isServer)
        {
            Type = ObservableFieldType.Vector2;
            Value = value;
            if (IsNetworkSynced && IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetVector2(Vector2 value)
        {
            if (Type == ObservableFieldType.Vector2)
            {
                if (Value != value)
                {
                    var old = Value;
                    Value = value;
                    ValueChanged?.Invoke(this, Value - old);
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

        public void ExternalSet(Vector2 value)
        {
            if (SetVector2(value))
            {
                if (IsNetworkSynced && IsServer)
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
                w.WriteVector2(Value);
                IsServerDirty = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool Deserialize(NetworkReader r)
        {
            return base.Deserialize(r) && SetVector2(r.ReadVector2());
        }
        #endregion

        #region - Saving -
        public override SavedObservable Save()
        {
            return new SavedObservable(FieldID, Type, $"{Value.x},{Value.y}");
        }
        #endregion
    }
}