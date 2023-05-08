using Sirenix.OdinInspector.Editor.GettingStarted;
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
            IsServer = @class.IsServer;
        }

        public ObservableField(int fieldID, bool saveValue, bool isServer)
        {
            Class = null;
            FieldID = fieldID;
            SaveValue = saveValue;
            IsServer = isServer;
        }        

        #region - Serialization -
        public virtual bool Serialize(NetworkWriter w, bool clearDirtyFlag = true)
        {
            if (IsServer && IsServerDirty)
            {
                w.WriteInt(FieldID);
                w.WriteByte((byte)Type);
                return true;
            }
            return false;
        }

        public virtual bool SerializeInFull(NetworkWriter w)
        {
            if (IsServer)
            {
                w.WriteInt(FieldID);
                w.WriteByte((byte)Type);
                return true;
            }
            return false;
        }

        public virtual bool Deserialize(NetworkReader r)
        {
            return !IsServer;
        }

        public static void StartDeserialize(NetworkReader r, out int id, out ObservableFieldType type)
        {
            id = r.ReadInt();
            type = (ObservableFieldType)r.ReadByte();
        }
        #endregion

        #region - Saving & Loading -
        public abstract SavedObservable Save();

        public void Load(SavedObservable observable)
        {
            if (observable.Value is null or "") { return; }

            switch (observable.Type)
            {
                case ObservableFieldType.Byte:
                    if (this is ByteField bf)
                    {
                        bf.ExternalSet(byte.Parse(observable.Value));
                    }
                    break;
                case ObservableFieldType.Short:
                    if (this is ShortField sf)
                    {
                        sf.ExternalSet(short.Parse(observable.Value));
                    }
                    break;
                case ObservableFieldType.UShort:
                    if (this is UShortField usf)
                    {
                        usf.ExternalSet(ushort.Parse(observable.Value));
                    }
                    break;
                case ObservableFieldType.Int:
                    if (this is IntField intF)
                    {
                        intF.ExternalSet(int.Parse(observable.Value));
                    }
                    break;
                case ObservableFieldType.UInt:
                    if (this is UIntField uintF)
                    {
                        uintF.ExternalSet(uint.Parse(observable.Value));
                    }
                    break;
                case ObservableFieldType.Float:
                    if (this is FloatField ff)
                    {
                        ff.ExternalSet(float.Parse(observable.Value));
                    }
                    break;
                case ObservableFieldType.Long:
                    if (this is LongField lf)
                    {
                        lf.ExternalSet(long.Parse(observable.Value));
                    }
                    break;
                case ObservableFieldType.ULong:
                    if (this is ULongField ulf)
                    {
                        ulf.ExternalSet(ulong.Parse(observable.Value));
                    }
                    break;
                case ObservableFieldType.Double:
                    if (this is DoubleField df)
                    {
                        df.ExternalSet(double.Parse(observable.Value));
                    }
                    break;
                case ObservableFieldType.Vector2:
                    if (this is Vector2Field v2f)
                    {
                        string[] split2 = observable.Value.Split(new char[] { ',' });
                        v2f.ExternalSet(new Vector2(float.Parse(split2[0]), float.Parse(split2[1])));
                    }
                    break;
                case ObservableFieldType.Vector2Int:
                    if (this is Vector2IntField v2if)
                    {
                        string[] split2i = observable.Value.Split(new char[] { ',' });
                        v2if.ExternalSet(new Vector2Int(int.Parse(split2i[0]), int.Parse(split2i[1])));
                    }
                    break;
                case ObservableFieldType.Vector3:
                    if (this is Vector3Field v3f)
                    {
                        string[] split3 = observable.Value.Split(new char[] { ',' });
                        v3f.ExternalSet(new Vector3(float.Parse(split3[0]), float.Parse(split3[1]), float.Parse(split3[2])));
                    }
                    break;
                case ObservableFieldType.Vector3Int:
                    if (this is Vector3IntField v3if)
                    {
                        string[] split3i = observable.Value.Split(new char[] { ',' });
                        v3if.ExternalSet(new Vector3Int(int.Parse(split3i[0]), int.Parse(split3i[1]), int.Parse(split3i[2])));
                    }
                    break;
                case ObservableFieldType.Vector3DeltaCompressed:
                    if (this is Vector3DeltaCompressedField v3df)
                    {
                        string[] split3 = observable.Value.Split(new char[] { ',' });
                        v3df.ExternalSet(new Vector3(float.Parse(split3[0]), float.Parse(split3[1]), float.Parse(split3[2])));
                    }
                    break;
                case ObservableFieldType.Vector4:
                    if (this is Vector4Field v4f)
                    {
                        string[] split4 = observable.Value.Split(new char[] { ',' });
                        v4f.ExternalSet(new Vector4(float.Parse(split4[0]), float.Parse(split4[1]), float.Parse(split4[2]), float.Parse(split4[3])));
                    }
                    break;
                case ObservableFieldType.Color:
                    if (this is ColorField colorf)
                    {
                        string[] color = observable.Value.Split(new char[] { ',' });
                        colorf.ExternalSet(new Color(float.Parse(color[0]), float.Parse(color[1]), float.Parse(color[2]), float.Parse(color[3])));
                    }
                    break;
                case ObservableFieldType.Quaternion:
                    if (this is QuaternionField qf)
                    {
                        string[] quat = observable.Value.Split(new char[] { ',' });
                        qf.ExternalSet(new Quaternion(float.Parse(quat[0]), float.Parse(quat[1]), float.Parse(quat[2]), float.Parse(quat[3])));
                    }
                    break;
                case ObservableFieldType.CompressedQuaternion:
                    if (this is CompressedQuaternionField cqf)
                    {
                        string[] cquat = observable.Value.Split(new char[] { ',' });
                        cqf.ExternalSet(new Quaternion(float.Parse(cquat[0]), float.Parse(cquat[1]), float.Parse(cquat[2]), float.Parse(cquat[3])));
                    }
                    break;
                case ObservableFieldType.String:
                    if (this is StringField stf)
                    {
                        stf.ExternalSet(observable.Value);
                    }
                    break;                               
            }
        }
        #endregion

        #region - Statics -
        public static ObservableField GetFieldByType(int fieldID, ObservableFieldType type, bool saveValue, bool isServer)
        {
            return type switch
            {
                ObservableFieldType.Byte => new ByteField(fieldID, saveValue, isServer, 0),
                ObservableFieldType.Short => new ShortField(fieldID, saveValue, isServer, 0),
                ObservableFieldType.UShort => new UShortField(fieldID, saveValue, isServer, 0),
                ObservableFieldType.Int => new IntField(fieldID, saveValue, isServer, 0),
                ObservableFieldType.UInt => new UIntField(fieldID, saveValue, isServer, 0),
                ObservableFieldType.Float => new FloatField(fieldID, saveValue, isServer, 0),
                ObservableFieldType.Long => new LongField(fieldID, saveValue, isServer, 0),
                ObservableFieldType.ULong => new ULongField(fieldID, saveValue, isServer, 0),
                ObservableFieldType.Double => new DoubleField(fieldID, saveValue, isServer, 0),
                ObservableFieldType.Vector2 => new Vector2Field(fieldID, saveValue, isServer, Vector2.zero),
                ObservableFieldType.Vector2Int => new Vector2IntField(fieldID, saveValue, isServer, Vector2Int.zero),
                ObservableFieldType.Vector3 => new Vector3Field(fieldID, saveValue, isServer, Vector3.zero),
                ObservableFieldType.Vector3Int => new Vector3IntField(fieldID, saveValue, isServer, Vector3Int.zero),
                ObservableFieldType.Vector3DeltaCompressed => new Vector3DeltaCompressedField(fieldID, saveValue, isServer, Vector3.zero),
                ObservableFieldType.Vector4 => new Vector4Field(fieldID, saveValue, isServer, Vector4.zero),
                ObservableFieldType.Color => new ColorField(fieldID, saveValue, isServer, Color.white),
                ObservableFieldType.Quaternion => new QuaternionField(fieldID, saveValue, isServer, Quaternion.identity),
                ObservableFieldType.CompressedQuaternion => new CompressedQuaternionField(fieldID, saveValue, isServer, Quaternion.identity),
                ObservableFieldType.String => new StringField(fieldID, saveValue, isServer, ""),
                _ => null,
            };
        }
        #endregion
    }
}
