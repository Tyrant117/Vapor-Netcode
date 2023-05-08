﻿using System;

namespace VaporNetcode
{
    [Serializable]
    public class ULongField : ObservableField
    {
        public static implicit operator ulong(ULongField f) => f.Value;

        public ulong Value { get; protected set; }
        public event Action<ULongField> ValueChanged;

        public ULongField(ObservableClass @class, int fieldID, bool saveValue, ulong value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.ULong;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public ULongField(int fieldID, bool saveValue, bool isServer, ulong value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.ULong;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetULong(ulong value)
        {
            if (Type == ObservableFieldType.ULong)
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

        internal bool ModifyULong(ulong value, ObservableModifyType type)
        {
            return type switch
            {
                ObservableModifyType.Set => SetULong(value),
                ObservableModifyType.Add => SetULong(Value + value),
                ObservableModifyType.Percent => SetULong(Value * value),
                ObservableModifyType.PercentAdd => SetULong(Value + Value * value),
                _ => false,
            };
        }

        public void ExternalSet(ulong value)
        {
            if (SetULong(value))
            {
                if (IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }

        public void ExternalModify(ulong value, ObservableModifyType type)
        {
            if (ModifyULong(value, type))
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
            if (base.Serialize(w))
            {
                Compression.CompressVarUInt(w, Value);
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
            return base.Deserialize(r) && SetULong(Compression.DecompressVarUInt(r));
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