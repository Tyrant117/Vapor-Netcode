using System;
using UnityEngine;

namespace VaporNetcode
{
    public class Vector2IntField : ObservableField
    {
        public static implicit operator Vector2Int(Vector2IntField f) => f.Value;
        public Vector2Int Value { get; protected set; }
        public event Action<Vector2IntField, Vector2Int> ValueChanged;

        public Vector2IntField(ObservableClass @class, int fieldID, bool saveValue, Vector2Int value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector2Int;
            Value = value;
            if (IsNetworkSynced && IsServer)
            {
                IsServerDirty = true;
            }
        }

        public Vector2IntField(int fieldID, bool saveValue, bool isNetworkSynced, bool isServer, Vector2Int value) : base(fieldID, saveValue, isNetworkSynced, isServer)
        {
            Type = ObservableFieldType.Vector2Int;
            Value = value;
            if (IsNetworkSynced && IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetVector2Int(Vector2Int value)
        {
            if (Type == ObservableFieldType.Vector2Int)
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

        public void ExternalSet(Vector2Int value)
        {
            if (SetVector2Int(value))
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
                w.WriteVector2Int(Value);
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
            return base.Deserialize(r) && SetVector2Int(r.ReadVector2Int());
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