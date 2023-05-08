using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public class UIntField : ObservableField
    {
        public static implicit operator uint(UIntField f) => f.Value;

        public uint Value { get; protected set; }
        public event Action<UIntField> ValueChanged;

        public UIntField(ObservableClass @class, int fieldID, bool saveValue, uint value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.UInt;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public UIntField(int fieldID, bool saveValue, bool isServer, uint value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.UInt;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetInt(uint value)
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

        public void ExternalSet(uint value)
        {
            if (SetInt(value))
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
        public override bool Serialize(NetworkWriter w, bool doNotMarkDirty = false)
        {
            if (base.Serialize(w))
            {
                w.WriteUInt(Value);
                if (!doNotMarkDirty)
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
            return Serialize(w, true);
        }

        public override bool Deserialize(NetworkReader r)
        {
            return base.Deserialize(r) && SetInt(r.ReadUInt());
        }
        #endregion

        #region - Saving -
        public override SavedObservable Save()
        {
            return new SavedObservable(FieldID, Type, Value.ToString());
        }
        #endregion
    }
}
