using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public class ColorField : ObservableField
    {
        public static implicit operator Color(ColorField f) => f.Value;

        public Color Value { get; protected set; }
        public event Action<ColorField> ValueChanged;

        public ColorField(ObservableClass @class, int fieldID, bool saveValue, Color value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Color;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public ColorField(int fieldID, bool saveValue, bool isServer, Color value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.Color;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetColor(Color value)
        {
            if (Type == ObservableFieldType.Color)
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

        public void ExternalSet(Color value)
        {
            if (SetColor(value))
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
                w.WriteColor(Value);
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
            return base.Deserialize(r) && SetColor(r.ReadColor());
        }
        #endregion

        #region - Saving -
        public override SavedObservable Save()
        {
            return new SavedObservable(FieldID, Type, $"{Value.r},{Value.g},{Value.b},{Value.a}");
        }
        #endregion
    }
}
