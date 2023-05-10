﻿using System;

namespace VaporNetcode
{
    [Serializable]
    public class FloatField : SyncField
    {
        public static implicit operator float(FloatField f) => f.Value;

        public float Value { get; protected set; }
        public event Action<FloatField, float> ValueChanged;

        public FloatField(SyncClass @class, int fieldID, bool saveValue, float value) : base(@class, fieldID, saveValue)
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
        public override bool Serialize(NetworkWriter w, bool clearDirtyFlag = true)
        {
            if (base.Serialize(w, clearDirtyFlag))
            {
                w.WriteFloat(Value);
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