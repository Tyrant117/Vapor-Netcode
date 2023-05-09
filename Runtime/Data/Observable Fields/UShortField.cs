using System;

namespace VaporNetcode
{
    public class UShortField : ObservableField
    {
        public static implicit operator ushort(UShortField f) => f.Value;

        public ushort Value { get; protected set; }
        public event Action<UShortField> ValueChanged;

        public UShortField(ObservableClass @class, int fieldID, bool saveValue, ushort value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.UShort;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public UShortField(int fieldID, bool saveValue, bool isServer, ushort value) : base(fieldID, saveValue, isServer)
        {
            Type = ObservableFieldType.UShort;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetShort(ushort value)
        {
            if (Type == ObservableFieldType.UShort)
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

        public void ExternalSet(ushort value)
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
        #endregion

        #region - Serialization -
        public override bool Serialize(NetworkWriter w, bool clearDirtyFlag = true)
        {
            if (base.Serialize(w, clearDirtyFlag))
            {
                w.WriteUShort(Value);
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
            return base.Deserialize(r) && SetShort(r.ReadUShort());
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
