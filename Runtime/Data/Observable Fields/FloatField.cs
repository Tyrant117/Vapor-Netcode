using System;

namespace VaporNetcode
{
    [Serializable]
    public class FloatField : ObservableField
    {
        public static implicit operator float(FloatField f) => f.Value;

        public float Value { get; protected set; }
        public event Action<FloatField, float> ValueChanged;

        public FloatField(ObservableClass @class, int fieldID, bool saveValue, float value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Float;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public FloatField(int fieldID, bool saveValue, bool isServer, float value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.Float;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetFloat(float value)
        {
            if (Type == ObservableFieldType.Float)
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

        internal bool ModifyFloat(float value, ObservableModifyType type)
        {
            return type switch
            {
                ObservableModifyType.Set => SetFloat(value),
                ObservableModifyType.Add => SetFloat(Value + value),
                ObservableModifyType.Percent => SetFloat(Value * value),
                ObservableModifyType.PercentAdd => SetFloat(Value + Value * value),
                _ => false,
            };
        }

        public void ExternalSet(float value)
        {
            if (SetFloat(value))
            {
                if (IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }

        public void ExternalModify(float value, ObservableModifyType type)
        {
            if (ModifyFloat(value, type))
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
                w.WriteFloat(Value);
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
            return base.Deserialize(r) && SetFloat(r.ReadFloat());
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