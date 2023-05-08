using System;

namespace VaporNetcode
{
    [Serializable]
    public class DoubleField : ObservableField
    {
        public static implicit operator double(DoubleField f) => f.Value;

        public double Value { get; protected set; }
        public event Action<DoubleField, double> ValueChanged;

        public DoubleField(ObservableClass @class, int fieldID, bool saveValue, double value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Double;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public DoubleField(int fieldID, bool saveValue, bool isServer, double value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.Double;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }


        #region - Setters -
        internal bool SetDouble(double value)
        {
            if (Type == ObservableFieldType.Double)
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
        internal bool ModifyDouble(double value, ObservableModifyType type)
        {
            return type switch
            {
                ObservableModifyType.Set => SetDouble(value),
                ObservableModifyType.Add => SetDouble(Value + value),
                ObservableModifyType.Percent => SetDouble(Value * value),
                ObservableModifyType.PercentAdd => SetDouble(Value + Value * value),
                _ => false,
            };
        }

        public void ExternalSet(double value)
        {
            if (SetDouble(value))
            {
                if (IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }

        public void ExternalModify(double value, ObservableModifyType type)
        {
            if (ModifyDouble(value, type))
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
                w.WriteDouble(Value);
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
            return base.Deserialize(r) && SetDouble(r.ReadDouble());
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