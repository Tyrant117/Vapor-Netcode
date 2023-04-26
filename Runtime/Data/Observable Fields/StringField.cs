using System;

namespace VaporNetcode
{
    [Serializable]
    public class StringField : ObservableField
    {
        public static implicit operator string(StringField f) => f.Value;

        public string Value { get; protected set; }
        /// <summary>
        /// Returns the new and old values of the changed string. New -> Old
        /// </summary>
        public event Action<StringField, string> ValueChanged;

        public StringField(ObservableClass @class, int fieldID, bool saveValue, string value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.String;
            Value = value;
            if (IsNetworkSynced && IsServer)
            {
                IsServerDirty = true;
            }
        }

        public StringField(int fieldID, bool saveValue, bool isNetworkSynced, bool isServer, string value) : base(fieldID, saveValue, isNetworkSynced, isServer)
        {
            Type = ObservableFieldType.String;
            Value = value;
            if (IsNetworkSynced && IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetString(string value)
        {
            if (Type == ObservableFieldType.String)
            {
                if (Value != value)
                {
                    var oldValue = Value;
                    Value = value;
                    ValueChanged?.Invoke(this, oldValue);
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

        public void ExternalSet(string value)
        {
            if (SetString(value))
            {
                if (IsNetworkSynced && IsServer)
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
                w.WriteString(Value);
                IsServerDirty = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool Deserialize(NetworkReader r)
        {
            return base.Deserialize(r) && SetString(r.ReadString());
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