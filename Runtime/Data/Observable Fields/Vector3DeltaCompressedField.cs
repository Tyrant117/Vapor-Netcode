using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public class Vector3DeltaCompressedField : ObservableField
    {
        public static implicit operator Vector3(Vector3DeltaCompressedField f) => f.Value;

        public Vector3 Value { get; protected set; }
        public float Precision { get; set; } = 0.01f;
        public event Action<Vector3DeltaCompressedField, Vector3> ValueChanged;

        private Vector3Long _lastSerializedValue;
        private Vector3Long _lastDeserializedValue;

        public Vector3DeltaCompressedField(ObservableClass @class, int fieldID, bool saveValue, Vector3 value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector3DeltaCompressed;
            Value = value;
            Debug.Log($"V3D Created {IsServer} {IsServerDirty}");
            if (IsServer)
            {
                IsServerDirty = true;
                Debug.Log($"V3D Dirtied {IsServerDirty}");
            }
        }

        public Vector3DeltaCompressedField(int fieldID, bool saveValue, bool isServer, Vector3 value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.Vector3DeltaCompressed;
            Value = value;
            Debug.Log($"V3D Created {IsServer} {IsServerDirty}");
            if (IsServer)
            {
                IsServerDirty = true;
                Debug.Log($"V3D Dirtied {IsServerDirty}");
            }
        }

        #region - Setters -
        internal bool SetVector3(Vector3 value)
        {
            if (Type == ObservableFieldType.Vector3DeltaCompressed)
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

        public void ExternalSet(Vector3 value)
        {
            if (SetVector3(value))
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
                Compression.ScaleToLong(Value, Precision, out Vector3Long quantized);
                var deltaPos = quantized - _lastSerializedValue;

                w.WriteBool(true); // is delta
                Compression.CompressVarInt(w, deltaPos.x);
                Compression.CompressVarInt(w, deltaPos.y);
                Compression.CompressVarInt(w, deltaPos.z);
                Compression.ScaleToLong(Value, Precision, out _lastSerializedValue);
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
            if (base.SerializeInFull(w))
            {
                w.WriteBool(false);
                w.WriteVector3(Value);
                IsServerDirty = false;
                Debug.Log($"V3D {FieldID} {Type} {false} {Value}");
                return true;
            }
            else
            {
                Debug.Log($"V3D Cannot Serialize {IsServer} {IsServerDirty}");
                return false;
            }
        }

        public override bool Deserialize(NetworkReader r)
        {
            if (!base.Deserialize(r)) { return false; }
            bool delta = r.ReadBool();
            Debug.Log($"V3 Delta {delta}");
            bool set;
            if (delta)
            {
                Vector3Long deltaValue = new(Compression.DecompressVarInt(r), Compression.DecompressVarInt(r), Compression.DecompressVarInt(r));
                Vector3Long quantized = _lastDeserializedValue + deltaValue;
                set = SetVector3(Compression.ScaleToFloat(quantized, Precision));
                Compression.ScaleToLong(Value, Precision, out _lastDeserializedValue);
            }
            else
            {
                set = SetVector3(r.ReadVector3());
                Compression.ScaleToLong(Value, Precision, out _lastDeserializedValue);
            }
            return set;
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
