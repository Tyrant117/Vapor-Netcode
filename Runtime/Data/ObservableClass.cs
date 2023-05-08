using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    [Serializable]
    public struct SavedObservableClass
    {
        public int Type;
        public int ID;
        public SavedObservable[] SavedFields;

        public SavedObservableClass(int type, int id, List<SavedObservable> fields)
        {
            Type = type;
            ID = id;
            SavedFields = fields.ToArray();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteInt(Type);
            w.WriteInt(ID);
            w.WriteInt(SavedFields.Length);
            for (int i = 0; i < SavedFields.Length; i++)
            {
                SavedFields[i].Serialize(w);
            }
        }

        public static SavedObservableClass Deserialize(NetworkReader r)
        {
            var type = r.ReadInt();
            var id = r.ReadInt();
            var length = r.ReadInt();
            var fields = new SavedObservable[length];
            for (int i = 0; i < length; i++)
            {
                fields[i] = SavedObservable.Deserialize(r);
            }
            return new SavedObservableClass()
            {
                Type = type,
                ID = id,
                SavedFields = fields
            };
        }
    }

    public static class ObservableClassID<T> where T : ObservableClass
    {
        public static readonly int ID = typeof(T).Name.GetStableHashCode();
    }

    public abstract class ObservableClass
    {
        public bool Dirty => dirtyFields.Count > 0;
        public int Type { get; protected set; }
        public int ID { get; protected set; }
        public bool IsServer { get; }
        public ObservableField GetField(int fieldID) => fields[fieldID];
        public T GetField<T>(int fieldID) where T : ObservableField => (T)fields[fieldID];

        protected Dictionary<int, ObservableField> fields = new();
        protected List<int> dirtyFields = new();
        protected HashSet<int> hashedDirties = new();

        public event Action<ObservableClass> Dirtied;
        public event Action<ObservableClass> Changed;

        public ObservableClass(int unqiueID, bool isServer)
        {
            ID = unqiueID;
            IsServer = isServer;
        }

        public ObservableClass(int containerType, int unqiueID, bool isServer)
        {
            Type = containerType;
            ID = unqiueID;
            IsServer = isServer;
        }

        #region - Field Management -
        public void AddField(int fieldID, ObservableFieldType type, bool saveValue, object value = null)
        {
            ObservableField field = value == null ? AddFieldByType(fieldID, type, saveValue) : AddFieldByType(fieldID, type, saveValue, value);
            if (field != null)
            {
                fields[fieldID] = field;
                MarkDirty(fields[fieldID]);
            }
            else
            {
                Debug.Log($"Class {Type} - {ID} Failed To Add Field: {type} {fieldID}");
            }
        }

        public void AddField(ObservableField field)
        {
            fields[field.FieldID] = field;
            MarkDirty(field);
        }

        protected ObservableField AddFieldByType(int fieldID, ObservableFieldType type, bool saveValue, object value)
        {
            return type switch
            {
                ObservableFieldType.Byte => new ByteField(this, fieldID, saveValue, Convert.ToByte(value)),
                ObservableFieldType.Short => new ShortField(this, fieldID, saveValue, Convert.ToInt16(value)),
                ObservableFieldType.UShort => new UShortField(this, fieldID, saveValue, Convert.ToUInt16(value)),
                ObservableFieldType.Int => new IntField(this, fieldID, saveValue, Convert.ToInt32(value)),
                ObservableFieldType.UInt => new UIntField(this, fieldID, saveValue, Convert.ToUInt32(value)),
                ObservableFieldType.Float => new FloatField(this, fieldID, saveValue, Convert.ToSingle(value)),
                ObservableFieldType.Long => new LongField(this, fieldID, saveValue, Convert.ToInt64(value)),
                ObservableFieldType.ULong => new ULongField(this, fieldID, saveValue, Convert.ToUInt64(value)),
                ObservableFieldType.Double => new DoubleField(this, fieldID, saveValue, Convert.ToDouble(value)),
                ObservableFieldType.Vector2 => new Vector2Field(this, fieldID, saveValue, (Vector2)value),
                ObservableFieldType.Vector2Int => new Vector2IntField(this, fieldID, saveValue, (Vector2Int)value),
                ObservableFieldType.Vector3 => new Vector3Field(this, fieldID, saveValue, (Vector3)value),
                ObservableFieldType.Vector3Int => new Vector3IntField(this, fieldID, saveValue, (Vector3Int)value),
                ObservableFieldType.Vector3DeltaCompressed => new Vector3DeltaCompressedField(this, fieldID, saveValue, (Vector3)value),
                ObservableFieldType.Vector4 => new Vector4Field(this, fieldID, saveValue, (Vector4)value),
                ObservableFieldType.Color => new ColorField(this, fieldID, saveValue, (Color)value),
                ObservableFieldType.Quaternion => new QuaternionField(this, fieldID, saveValue, (Quaternion)value),
                ObservableFieldType.CompressedQuaternion => new CompressedQuaternionField(this, fieldID, saveValue, (Quaternion)value),
                ObservableFieldType.String => new StringField(this, fieldID, saveValue, Convert.ToString(value)),
                _ => null,
            };
        }

        protected ObservableField AddFieldByType(int fieldID, ObservableFieldType type, bool saveValue)
        {

            return type switch
            {
                ObservableFieldType.Byte => new ByteField(this, fieldID, saveValue, 0),
                ObservableFieldType.Short => new ShortField(this, fieldID, saveValue, 0),
                ObservableFieldType.UShort => new UShortField(this, fieldID, saveValue, 0),
                ObservableFieldType.Int => new IntField(this, fieldID, saveValue, 0),
                ObservableFieldType.UInt => new UIntField(this, fieldID, saveValue, 0),
                ObservableFieldType.Float => new FloatField(this, fieldID, saveValue, 0),
                ObservableFieldType.Long => new LongField(this, fieldID, saveValue, 0),
                ObservableFieldType.ULong => new ULongField(this, fieldID, saveValue, 0),
                ObservableFieldType.Double => new DoubleField(this, fieldID, saveValue, 0),
                ObservableFieldType.Vector2 => new Vector2Field(this, fieldID, saveValue, Vector2.zero),
                ObservableFieldType.Vector2Int => new Vector2IntField(this, fieldID, saveValue, Vector2Int.zero),
                ObservableFieldType.Vector3 => new Vector3Field(this, fieldID, saveValue, Vector3.zero),
                ObservableFieldType.Vector3Int => new Vector3IntField(this, fieldID, saveValue, Vector3Int.zero),
                ObservableFieldType.Vector3DeltaCompressed => new Vector3DeltaCompressedField(this, fieldID, saveValue, Vector3.zero),
                ObservableFieldType.Vector4 => new Vector4Field(this, fieldID, saveValue, Vector4.zero),
                ObservableFieldType.Color => new ColorField(this, fieldID, saveValue, Color.white),
                ObservableFieldType.Quaternion => new QuaternionField(this, fieldID, saveValue, Quaternion.identity),
                ObservableFieldType.CompressedQuaternion => new CompressedQuaternionField(this, fieldID, saveValue, Quaternion.identity),
                ObservableFieldType.String => new StringField(this, fieldID, saveValue, ""),
                _ => null,
            };
        }

        internal virtual void MarkDirty(ObservableField field)
        {
            if (IsServer && !hashedDirties.Contains(field.FieldID))
            {
                bool wasDirty = Dirty;
                dirtyFields.Add(field.FieldID);
                hashedDirties.Add(field.FieldID);
                if (Dirty && !wasDirty)
                {
                    Dirtied?.Invoke(this);
                }
            }
        }
        #endregion

        #region - Serialization -
        public virtual void Serialize(NetworkWriter w, bool doNotMarkDirty = false)
        {
            if (!IsServer || !Dirty) { return; }

            w.WriteInt(Type);
            w.WriteInt(ID);
            int count = dirtyFields.Count;
            w.WriteInt(count);
            for (int i = 0; i < count; i++)
            {
                fields[dirtyFields[i]].Serialize(w, doNotMarkDirty);
            }
            if (doNotMarkDirty)
            {
                dirtyFields.Clear();
                hashedDirties.Clear();
            }

            // Call this after the fields are cleared so if the contents change again it will dirty again for the next pass.
            if (count > 0)
            {
                Changed?.Invoke(this);
            }
        }

        public virtual void Deserialize(NetworkReader r)
        {
            if (IsServer) { return; }

            int count = r.ReadInt();
            Debug.Log($"{Type} - {ID} Deserializing Class Fields: {count}");
            for (int i = 0; i < count; i++)
            {
                ObservableField.StartDeserialize(r, out int fieldID, out ObservableFieldType type);
                Debug.Log($"Class {Type} - {ID} Trying To Add Field: {type} {fieldID}");
                if (fields.ContainsKey(fieldID))
                {
                    fields[fieldID].Deserialize(r);
                }
                else
                {
                    AddField(fieldID, type, false);
                    fields[fieldID].Deserialize(r);
                }
            }
            if (count > 0)
            {
                Changed?.Invoke(this);
            }
        }

        public virtual void SerializeInFull(NetworkWriter w)
        {
            w.WriteInt(Type);
            w.WriteInt(ID);
            int count = fields.Count;
            w.WriteInt(count);
            Debug.Log($"Batching Class: Type: {Type} ID: {ID} Count: {count}");
            foreach (var item in fields.Values)
            {
                item.SerializeInFull(w);
            }
        }

        public static void StartDeserialize(NetworkReader r, out int type, out int id)
        {
            type = r.ReadInt();
            id = r.ReadInt();
        }
        #endregion

        #region - Saving & Loading -
        public SavedObservableClass Save()
        {
            List<SavedObservable> holder = new();
            foreach (var field in fields.Values)
            {
                if (field.SaveValue)
                {
                    holder.Add(field.Save());
                }
            }
            return new SavedObservableClass(Type, ID, holder);
        }

        public void Load(SavedObservableClass save, bool createMissingFields = true)
        {
            foreach (var field in save.SavedFields)
            {
                if (fields.ContainsKey(field.ID))
                {
                    SetFromString(field.ID, field.Value);
                }
                else
                {
                    if (!createMissingFields) { continue; }
                    AddField(field.ID, field.Type, true);
                    SetFromString(field.ID, field.Value);
                }
            }
        }

        protected void SetFromString(int fieldID, string value)
        {
            if (value is null or "") { return; }
            if (!fields.ContainsKey(fieldID)) { return; }

            switch (fields[fieldID].Type)
            {
                case ObservableFieldType.Byte:
                    GetField<ByteField>(fieldID).ExternalSet(byte.Parse(value));
                    break;
                case ObservableFieldType.Short:
                    GetField<ShortField>(fieldID).ExternalSet(short.Parse(value));
                    break;
                case ObservableFieldType.UShort:
                    GetField<UShortField>(fieldID).ExternalSet(ushort.Parse(value));
                    break;
                case ObservableFieldType.Int:
                    GetField<IntField>(fieldID).ExternalSet(int.Parse(value));
                    break;
                case ObservableFieldType.UInt:
                    GetField<UIntField>(fieldID).ExternalSet(uint.Parse(value));
                    break;
                case ObservableFieldType.Float:
                    GetField<FloatField>(fieldID).ExternalSet(float.Parse(value));
                    break;
                case ObservableFieldType.Long:
                    GetField<LongField>(fieldID).ExternalSet(long.Parse(value));
                    break;
                case ObservableFieldType.ULong:
                    GetField<ULongField>(fieldID).ExternalSet(ulong.Parse(value));
                    break;
                case ObservableFieldType.Double:
                    GetField<DoubleField>(fieldID).ExternalSet(double.Parse(value));
                    break;
                case ObservableFieldType.Vector2:
                    string[] split2 = value.Split(new char[] { ',' });
                    GetField<Vector2Field>(fieldID).ExternalSet(new Vector2(float.Parse(split2[0]), float.Parse(split2[1])));
                    break;
                case ObservableFieldType.Vector2Int:
                    string[] split2i = value.Split(new char[] { ',' });
                    GetField<Vector2IntField>(fieldID).ExternalSet(new Vector2Int(int.Parse(split2i[0]), int.Parse(split2i[1])));
                    break;
                case ObservableFieldType.Vector3:
                    string[] split3 = value.Split(new char[] { ',' });
                    GetField<Vector3Field>(fieldID).ExternalSet(new Vector3(float.Parse(split3[0]), float.Parse(split3[1]), float.Parse(split3[2])));
                    break;
                case ObservableFieldType.Vector3Int:
                    string[] split3i = value.Split(new char[] { ',' });
                    GetField<Vector3IntField>(fieldID).ExternalSet(new Vector3Int(int.Parse(split3i[0]), int.Parse(split3i[1]), int.Parse(split3i[2])));
                    break;
                case ObservableFieldType.Vector3DeltaCompressed:
                    string[] split3d = value.Split(new char[] { ',' });
                    GetField<Vector3DeltaCompressedField>(fieldID).ExternalSet(new Vector3(float.Parse(split3d[0]), float.Parse(split3d[1]), float.Parse(split3d[2])));
                    break;
                case ObservableFieldType.Vector4:
                    string[] split4 = value.Split(new char[] { ',' });
                    GetField<Vector4Field>(fieldID).ExternalSet(new Vector4(float.Parse(split4[0]), float.Parse(split4[1]), float.Parse(split4[2]), float.Parse(split4[3])));
                    break;
                case ObservableFieldType.Color:
                    string[] color = value.Split(new char[] { ',' });
                    GetField<ColorField>(fieldID).ExternalSet(new Color(float.Parse(color[0]), float.Parse(color[1]), float.Parse(color[2]), float.Parse(color[3])));
                    break;
                case ObservableFieldType.Quaternion:
                    string[] quat = value.Split(new char[] { ',' });
                    GetField<QuaternionField>(fieldID).ExternalSet(new Quaternion(float.Parse(quat[0]), float.Parse(quat[1]), float.Parse(quat[2]), float.Parse(quat[3])));
                    break;
                case ObservableFieldType.CompressedQuaternion:
                    string[] compQuat = value.Split(new char[] { ',' });
                    GetField<CompressedQuaternionField>(fieldID).ExternalSet(new Quaternion(float.Parse(compQuat[0]), float.Parse(compQuat[1]), float.Parse(compQuat[2]), float.Parse(compQuat[3])));
                    break;
                case ObservableFieldType.String:
                    GetField<StringField>(fieldID).ExternalSet(value);
                    break;                               
            }
        }
        #endregion
    }
}
