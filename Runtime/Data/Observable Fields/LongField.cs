using System;

namespace VaporNetcode
{
    [Serializable]
    public class LongField : ObservableField
    {
        public static implicit operator long(LongField f) => f.Value;

        public long Value { get; protected set; }
        public event Action<LongField, long> ValueChanged;

        public LongField(ObservableClass @class, int fieldID, bool saveValue, long value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Long;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public LongField(int fieldID, bool saveValue, bool isNetworkSynced, bool isServer, long value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.Long;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetLong(long value)
        {
            if (Type == ObservableFieldType.Long)
            {
                if (Value != value)
                {
                    var oldValue = Value;
                    Value = value;
                    ValueChanged?.Invoke(this, Value - oldValue);
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

        internal bool ModifyLong(long value, ObservableModifyType type)
        {
            return type switch
            {
                ObservableModifyType.Set => SetLong(value),
                ObservableModifyType.Add => SetLong(Value + value),
                ObservableModifyType.Percent => SetLong(Value * value),
                ObservableModifyType.PercentAdd => SetLong(Value + Value * value),
                _ => false,
            };
        }

        public void ExternalSet(long value)
        {
            if (SetLong(value))
            {
                if (IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }

        public void ExternalModify(long value, ObservableModifyType type)
        {
            if (ModifyLong(value, type))
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
                w.WriteLong(Value);
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
            return base.Deserialize(r) && SetLong(r.ReadLong());
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