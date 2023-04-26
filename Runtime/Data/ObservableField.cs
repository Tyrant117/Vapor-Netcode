using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    [Serializable]
    public struct SavedObservable
    {
        public int ID;
        public ObservableFieldType Type;
        public string Value;

        public SavedObservable(int id, ObservableFieldType type, string value)
        {
            ID = id;
            Type = type;
            Value = value;
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteInt(ID);
            w.WriteInt((int)Type);
            w.WriteString(Value);
        }

        public static SavedObservable Deserialize(NetworkReader r)
        {
            return new SavedObservable(r.ReadInt(), (ObservableFieldType)r.ReadInt(), r.ReadString());
        }
    }

    public abstract class ObservableField
    {
        public ObservableClass Class { get; }
        public int FieldID { get; }
        public ObservableFieldType Type { get; protected set; }
        public bool SaveValue { get; }
        public bool IsNetworkSynced { get; }
        public bool IsServer { get; }

        private bool isServerDirty;
        public bool IsServerDirty
        {
            get => isServerDirty;
            protected set
            {
                if (value && value != isServerDirty)
                {
                    Dirtied?.Invoke(this);
                }
                isServerDirty = value;
            }
        }

        public event Action<ObservableField> Dirtied;

        public ObservableField(ObservableClass @class, int fieldID, bool saveValue)
        {
            Class = @class;
            FieldID = fieldID;
            SaveValue = saveValue;
            IsNetworkSynced = @class.IsNetworkSynced;
            IsServer = @class.IsServer;
        }

        public ObservableField(int fieldID, bool saveValue, bool isNetworkSynced, bool isServer)
        {
            Class = null;
            FieldID = fieldID;
            SaveValue = saveValue;
            IsNetworkSynced = isNetworkSynced;
            IsServer = isServer;
        }        

        #region - Serialization -
        public virtual bool Serialize(NetworkWriter w)
        {
            if (IsNetworkSynced && IsServer && IsServerDirty)
            {
                w.WriteInt(FieldID);
                w.WriteByte((byte)Type);
                return true;
            }
            return false;
        }

        public virtual bool Deserialize(NetworkReader r)
        {
            return IsNetworkSynced && !IsServer;
        }

        public static void StartDeserialize(NetworkReader r, out int id, out ObservableFieldType type)
        {
            id = r.ReadInt();
            type = (ObservableFieldType)r.ReadByte();
        }
        #endregion

        #region - Saving & Loading -
        public abstract SavedObservable Save();
        #endregion

        #region - Statics -
        public static ObservableField GetFieldByType(int fieldID, ObservableFieldType type, bool saveValue, bool isNetworkSynced, bool isServer)
        {
            return type switch
            {
                ObservableFieldType.Byte => new ByteField(fieldID, saveValue, isNetworkSynced, isServer, 0),
                ObservableFieldType.Short => new ShortField(fieldID, saveValue, isNetworkSynced, isServer, 0),
                ObservableFieldType.UShort => new UShortField(fieldID, saveValue, isNetworkSynced, isServer, 0),
                ObservableFieldType.Int => new IntField(fieldID, saveValue, isNetworkSynced, isServer, 0),
                ObservableFieldType.Float => new FloatField(fieldID, saveValue, isNetworkSynced, isServer, 0),
                ObservableFieldType.Long => new LongField(fieldID, saveValue, isNetworkSynced, isServer, 0),
                ObservableFieldType.ULong => new ULongField(fieldID, saveValue, isNetworkSynced, isServer, 0),
                ObservableFieldType.Double => new DoubleField(fieldID, saveValue, isNetworkSynced, isServer, 0),
                ObservableFieldType.Vector2 => new Vector2Field(fieldID, saveValue, isNetworkSynced, isServer, Vector2.zero),
                ObservableFieldType.Vector2Int => new Vector2IntField(fieldID, saveValue, isNetworkSynced, isServer, Vector2Int.zero),
                ObservableFieldType.Vector3 => new Vector3Field(fieldID, saveValue, isNetworkSynced, isServer, Vector3.zero),
                ObservableFieldType.Vector3Int => new Vector3IntField(fieldID, saveValue, isNetworkSynced, isServer, Vector3Int.zero),
                ObservableFieldType.Vector4 => new Vector4Field(fieldID, saveValue, isNetworkSynced, isServer, Vector4.zero),
                ObservableFieldType.Color => new ColorField(fieldID, saveValue, isNetworkSynced, isServer, Color.white),
                ObservableFieldType.Quaternion => new QuaternionField(fieldID, saveValue, isNetworkSynced, isServer, Quaternion.identity),
                ObservableFieldType.CompressedQuaternion => new CompressedQuaternionField(fieldID, saveValue, isNetworkSynced, isServer, Quaternion.identity),
                ObservableFieldType.String => new StringField(fieldID, saveValue, isNetworkSynced, isServer, ""),
                _ => null,
            };
        }
        #endregion
    }
}
