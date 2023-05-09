using System;

namespace VaporNetcode
{
    [Serializable]
    public class ByteField : ObservableField
    {
        public static implicit operator byte(ByteField f) => f.Value;
        public static implicit operator bool(ByteField f) => f.Bool;

        public byte Value { get; protected set; }
        public bool Bool => Value != 0;
        public event Action<ByteField, int> ValueChanged;

        public ByteField(ObservableClass @class, int fieldID, bool saveValue, byte value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Byte;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public ByteField(int fieldID, bool saveValue, bool isServer, byte value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.Byte;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetByte(byte value)
        {
            if (Type == ObservableFieldType.Byte)
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

        internal bool ModifyByte(byte value, ObservableModifyType type)
        {
            return type switch
            {
                ObservableModifyType.Set => SetByte(value),
                ObservableModifyType.Add => SetByte((byte)(Value + value)),
                ObservableModifyType.Percent => SetByte((byte)(Value * value)),
                ObservableModifyType.PercentAdd => SetByte((byte)(Value + Value * value)),
                _ => false,
            };
        }

        public void ExternalSet(byte value)
        {
            if (SetByte(value))
            {
                if(IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }

        public void ExternalModify(byte value, ObservableModifyType type)
        {
            if(ModifyByte(value, type))
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
                w.WriteByte(Value);
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
            return base.Deserialize(r) && SetByte(r.ReadByte());
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