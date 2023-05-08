using System;

namespace VaporNetcode
{
    [Serializable]
    public class ShortField : ObservableField
    {
        public static implicit operator short(ShortField f) => f.Value;

        public short Value { get; protected set; }
        public event Action<ShortField, int> ValueChanged;

        public ShortField(ObservableClass @class, int fieldID, bool saveValue, short value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Short;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public ShortField(int fieldID, bool saveValue, bool isNetworkSynced, bool isServer, short value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.Short;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetShort(short value)
        {
            if (Type == ObservableFieldType.Short)
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

        internal bool ModifyShort(short value, ObservableModifyType type)
        {
            return type switch
            {
                ObservableModifyType.Set => SetShort(value),
                ObservableModifyType.Add => SetShort((short)(Value + value)),
                ObservableModifyType.Percent => SetShort((short)(Value * value)),
                ObservableModifyType.PercentAdd => SetShort((short)(Value + Value * value)),
                _ => false,
            };
        }

        public void ExternalSet(short value)
        {
            if (SetShort(value))
            {
                if (IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }

        public void ExternalModify(short value, ObservableModifyType type)
        {
            if (ModifyShort(value, type))
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
                w.WriteShort(Value);
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
            return base.Deserialize(r) && SetShort(r.ReadShort());
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