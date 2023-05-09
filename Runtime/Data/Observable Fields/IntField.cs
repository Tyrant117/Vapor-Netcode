using System;

namespace VaporNetcode
{
    [Serializable]
    public class IntField : ObservableField
    {
        public static implicit operator int(IntField f) => f.Value;

        public bool IsEqual(IntField other) => Value == other.Value;
        public static bool IsEqual(IntField lhs, IntField rhs) => lhs.Value == rhs.Value;

        public int Value { get; protected set; }
        public bool HasFlag(int flagToCheck) => (Value & flagToCheck) != 0;
        public event Action<IntField> ValueChanged;
        public event Action<IntField, int> DeltaChanged;


        public IntField(ObservableClass @class, int fieldID, bool saveValue, int value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Int;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public IntField(int fieldID, bool saveValue, bool isServer, int value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.Int;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetInt(int value)
        {
            if (Value != value)
            {
                DeltaChanged?.Invoke(this, value - Value);
                Value = value;
                ValueChanged?.Invoke(this);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool ModifyInt(int value, ObservableModifyType type)
        {
            return type switch
            {
                ObservableModifyType.Set => SetInt(value),
                ObservableModifyType.Add => SetInt(Value + value),
                ObservableModifyType.Percent => SetInt(Value * value),
                ObservableModifyType.PercentAdd => SetInt(Value + Value * value),
                _ => false,
            };
        }

        public void ExternalSet(int value)
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

        public void ExternalModify(int value, ObservableModifyType type)
        {
            if (ModifyInt(value, type))
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
                w.WriteInt(Value);
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
            return base.Deserialize(r) && SetInt(r.ReadInt());
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