﻿using System;
using UnityEngine;

namespace VaporNetcode
{
    public class Vector3IntField : ObservableField
    {
        public static implicit operator Vector3Int(Vector3IntField f) => f.Value;

        public Vector3Int Value { get; protected set; }
        public event Action<Vector3IntField, Vector3Int> ValueChanged;

        public Vector3IntField(ObservableClass @class, int fieldID, bool saveValue, Vector3Int value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector3Int;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public Vector3IntField(int fieldID, bool saveValue, bool isNetworkSynced, bool isServer, Vector3Int value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.Vector3Int;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetVector3Int(Vector3Int value)
        {
            if (Type == ObservableFieldType.Vector3Int)
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

        public void ExternalSet(Vector3Int value)
        {
            if (SetVector3Int(value))
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
                w.WriteVector3Int(Value);
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
            return base.Deserialize(r) && SetVector3Int(r.ReadVector3Int());
        }
        #endregion

        #region - Saving -
        public override SavedObservable Save()
        {
            return new SavedObservable(FieldID, Type, $"{Value.x},{Value.y},{Value.z}");
        }
        #endregion
    }
}