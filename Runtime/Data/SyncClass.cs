using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    [Serializable]
    public struct SavedSyncClass
    {
        public int Type;
        public int ID;
        public SavedSyncField[] SavedFields;

        public SavedSyncClass(int type, int id, List<SavedSyncField> fields)
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

        public static SavedSyncClass Deserialize(NetworkReader r)
        {
            var type = r.ReadInt();
            var id = r.ReadInt();
            var length = r.ReadInt();
            var fields = new SavedSyncField[length];
            for (int i = 0; i < length; i++)
            {
                fields[i] = SavedSyncField.Deserialize(r);
            }
            return new SavedSyncClass()
            {
                Type = type,
                ID = id,
                SavedFields = fields
            };
        }
    }

    public static class ObservableClassID<T> where T : SyncClass
    {
        public static readonly int ID = typeof(T).Name.GetStableHashCode();
    }

    public abstract class SyncClass
    {
        public bool Dirty => dirtyFields.Count > 0;
        public int Type { get; protected set; }
        public int ID { get; protected set; }
        public bool IsServer { get; }
        public SyncField GetField(int fieldID) => fields[fieldID];
        public T GetField<T>(int fieldID) where T : SyncField => (T)fields[fieldID];

        protected Dictionary<int, SyncField> fields = new();
        protected HashSet<int> dirtyFields = new();
        protected bool _isLoaded;

        public event Action<SyncClass> Dirtied;
        public event Action<SyncClass> Changed;

        public SyncClass(int unqiueID, bool isServer)
        {
            ID = unqiueID;
            IsServer = isServer;
        }

        public SyncClass(int containerType, int unqiueID, bool isServer)
        {
            Type = containerType;
            ID = unqiueID;
            IsServer = isServer;
        }

        #region - Field Management -
        public void AddField(int fieldID, SyncFieldType type, bool saveValue, object value = null)
        {
            SyncField field = value == null ? AddFieldByType(fieldID, type, saveValue) : AddFieldByType(fieldID, type, saveValue, value);
            if (field != null)
            {
                fields[fieldID] = field;
                MarkDirty(fields[fieldID]);
            }
            else
            {
                if (NetLogFilter.logWarn)
                {
                    Debug.Log($"Class {Type} - {ID} Failed To Add Field: {type} {fieldID}");
                }
            }
        }

        public void AddField(SyncField field)
        {
            fields[field.FieldID] = field;
            MarkDirty(field);
        }

        protected SyncField AddFieldByType(int fieldID, SyncFieldType type, bool saveValue, object value)
        {
            return type switch
            {
                SyncFieldType.Byte => new ByteField(this, fieldID, saveValue, Convert.ToByte(value)),
                SyncFieldType.Short => new ShortField(this, fieldID, saveValue, Convert.ToInt16(value)),
                SyncFieldType.UShort => new UShortField(this, fieldID, saveValue, Convert.ToUInt16(value)),
                SyncFieldType.Int => new IntField(this, fieldID, saveValue, Convert.ToInt32(value)),
                SyncFieldType.UInt => new UIntField(this, fieldID, saveValue, Convert.ToUInt32(value)),
                SyncFieldType.Float => new FloatField(this, fieldID, saveValue, Convert.ToSingle(value)),
                SyncFieldType.Long => new LongField(this, fieldID, saveValue, Convert.ToInt64(value)),
                SyncFieldType.ULong => new ULongField(this, fieldID, saveValue, Convert.ToUInt64(value)),
                SyncFieldType.Double => new DoubleField(this, fieldID, saveValue, Convert.ToDouble(value)),
                SyncFieldType.Vector2 => new Vector2Field(this, fieldID, saveValue, (Vector2)value),
                SyncFieldType.Vector2Int => new Vector2IntField(this, fieldID, saveValue, (Vector2Int)value),
                SyncFieldType.Vector3 => new Vector3Field(this, fieldID, saveValue, (Vector3)value),
                SyncFieldType.Vector3Int => new Vector3IntField(this, fieldID, saveValue, (Vector3Int)value),
                SyncFieldType.Vector3DeltaCompressed => new Vector3DeltaCompressedField(this, fieldID, saveValue, (Vector3)value),
                SyncFieldType.Vector4 => new Vector4Field(this, fieldID, saveValue, (Vector4)value),
                SyncFieldType.Color => new ColorField(this, fieldID, saveValue, (Color)value),
                SyncFieldType.Quaternion => new QuaternionField(this, fieldID, saveValue, (Quaternion)value),
                SyncFieldType.CompressedQuaternion => new CompressedQuaternionField(this, fieldID, saveValue, (Quaternion)value),
                SyncFieldType.String => new StringField(this, fieldID, saveValue, Convert.ToString(value)),
                _ => null,
            };
        }

        protected SyncField AddFieldByType(int fieldID, SyncFieldType type, bool saveValue)
        {

            return type switch
            {
                SyncFieldType.Byte => new ByteField(this, fieldID, saveValue, 0),
                SyncFieldType.Short => new ShortField(this, fieldID, saveValue, 0),
                SyncFieldType.UShort => new UShortField(this, fieldID, saveValue, 0),
                SyncFieldType.Int => new IntField(this, fieldID, saveValue, 0),
                SyncFieldType.UInt => new UIntField(this, fieldID, saveValue, 0),
                SyncFieldType.Float => new FloatField(this, fieldID, saveValue, 0),
                SyncFieldType.Long => new LongField(this, fieldID, saveValue, 0),
                SyncFieldType.ULong => new ULongField(this, fieldID, saveValue, 0),
                SyncFieldType.Double => new DoubleField(this, fieldID, saveValue, 0),
                SyncFieldType.Vector2 => new Vector2Field(this, fieldID, saveValue, Vector2.zero),
                SyncFieldType.Vector2Int => new Vector2IntField(this, fieldID, saveValue, Vector2Int.zero),
                SyncFieldType.Vector3 => new Vector3Field(this, fieldID, saveValue, Vector3.zero),
                SyncFieldType.Vector3Int => new Vector3IntField(this, fieldID, saveValue, Vector3Int.zero),
                SyncFieldType.Vector3DeltaCompressed => new Vector3DeltaCompressedField(this, fieldID, saveValue, Vector3.zero),
                SyncFieldType.Vector4 => new Vector4Field(this, fieldID, saveValue, Vector4.zero),
                SyncFieldType.Color => new ColorField(this, fieldID, saveValue, Color.white),
                SyncFieldType.Quaternion => new QuaternionField(this, fieldID, saveValue, Quaternion.identity),
                SyncFieldType.CompressedQuaternion => new CompressedQuaternionField(this, fieldID, saveValue, Quaternion.identity),
                SyncFieldType.String => new StringField(this, fieldID, saveValue, ""),
                _ => null,
            };
        }

        internal virtual void MarkDirty(SyncField field)
        {
            if (IsServer && dirtyFields.Add(field.FieldID))
            {
                //Debug.Log($"Class {Type} {ID} Dirty");
                Dirtied?.Invoke(this);
            }
            else
            {
                if (NetLogFilter.logDebug && NetLogFilter.syncVars && NetLogFilter.spew)
                {
                    Debug.Log($"Class {Type} {ID} Already Dirty");
                }
            }
        }
        #endregion

        #region - Serialization -
        public virtual void Serialize(NetworkWriter w, bool clearDirtyFlag = true)
        {
            if (!IsServer || !Dirty) { return; }

            w.WriteInt(Type);
            w.WriteInt(ID);
            int count = dirtyFields.Count;
            w.WriteInt(count);
            foreach (var df in dirtyFields)
            {
                fields[df].Serialize(w, clearDirtyFlag);
            }

            if (clearDirtyFlag)
            {
                dirtyFields.Clear();
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
            //Debug.Log($"{Type} - {ID} Deserializing Class Fields: {count}");
            for (int i = 0; i < count; i++)
            {
                SyncField.StartDeserialize(r, out int fieldID, out SyncFieldType type);
                if(NetLogFilter.logDebug && NetLogFilter.syncVars)
                {
                    Debug.Log($"Deserialize Class {Type} [{ID}] Field: {type} [{fieldID}] [{i+1}/{count}]");
                }
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

        public virtual void SerializeInFull(NetworkWriter w, bool clearDirtyFlag = true)
        {
            w.WriteInt(Type);
            w.WriteInt(ID);
            int count = fields.Count;
            w.WriteInt(count);
            if (NetLogFilter.logDebug && NetLogFilter.syncVars)
            {
                Debug.Log($"Batching Class: Type: {Type} ID: {ID} Count: {count}");
            }

            foreach (var item in fields.Values)
            {
                if (NetLogFilter.logDebug && NetLogFilter.syncVars)
                {
                    Debug.Log($"Batching Field: Type: {item.Type} ID: {item.FieldID}");
                }
                item.SerializeInFull(w, clearDirtyFlag);
            }

            if (clearDirtyFlag)
            {
                dirtyFields.Clear();

                if (count > 0)
                {
                    Changed?.Invoke(this);
                }
            }
        }

        public static void StartDeserialize(NetworkReader r, out int type, out int id)
        {
            type = r.ReadInt();
            id = r.ReadInt();
        }
        #endregion

        #region - Saving & Loading -
        public SavedSyncClass Save()
        {
            List<SavedSyncField> holder = new();
            foreach (var field in fields.Values)
            {
                if (field.SaveValue)
                {
                    holder.Add(field.Save());
                }
            }
            return new SavedSyncClass(Type, ID, holder);
        }

        public void Load(SavedSyncClass save, bool createMissingFields = true, bool forceReload = false)
        {
            if(_isLoaded && !forceReload) { return; }

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
                case SyncFieldType.Byte:
                    GetField<ByteField>(fieldID).ExternalSet(byte.Parse(value));
                    break;
                case SyncFieldType.Short:
                    GetField<ShortField>(fieldID).ExternalSet(short.Parse(value));
                    break;
                case SyncFieldType.UShort:
                    GetField<UShortField>(fieldID).ExternalSet(ushort.Parse(value));
                    break;
                case SyncFieldType.Int:
                    GetField<IntField>(fieldID).ExternalSet(int.Parse(value));
                    break;
                case SyncFieldType.UInt:
                    GetField<UIntField>(fieldID).ExternalSet(uint.Parse(value));
                    break;
                case SyncFieldType.Float:
                    GetField<FloatField>(fieldID).ExternalSet(float.Parse(value));
                    break;
                case SyncFieldType.Long:
                    GetField<LongField>(fieldID).ExternalSet(long.Parse(value));
                    break;
                case SyncFieldType.ULong:
                    GetField<ULongField>(fieldID).ExternalSet(ulong.Parse(value));
                    break;
                case SyncFieldType.Double:
                    GetField<DoubleField>(fieldID).ExternalSet(double.Parse(value));
                    break;
                case SyncFieldType.Vector2:
                    string[] split2 = value.Split(new char[] { ',' });
                    GetField<Vector2Field>(fieldID).ExternalSet(new Vector2(float.Parse(split2[0]), float.Parse(split2[1])));
                    break;
                case SyncFieldType.Vector2Int:
                    string[] split2i = value.Split(new char[] { ',' });
                    GetField<Vector2IntField>(fieldID).ExternalSet(new Vector2Int(int.Parse(split2i[0]), int.Parse(split2i[1])));
                    break;
                case SyncFieldType.Vector3:
                    string[] split3 = value.Split(new char[] { ',' });
                    GetField<Vector3Field>(fieldID).ExternalSet(new Vector3(float.Parse(split3[0]), float.Parse(split3[1]), float.Parse(split3[2])));
                    break;
                case SyncFieldType.Vector3Int:
                    string[] split3i = value.Split(new char[] { ',' });
                    GetField<Vector3IntField>(fieldID).ExternalSet(new Vector3Int(int.Parse(split3i[0]), int.Parse(split3i[1]), int.Parse(split3i[2])));
                    break;
                case SyncFieldType.Vector3DeltaCompressed:
                    string[] split3d = value.Split(new char[] { ',' });
                    GetField<Vector3DeltaCompressedField>(fieldID).ExternalSet(new Vector3(float.Parse(split3d[0]), float.Parse(split3d[1]), float.Parse(split3d[2])));
                    break;
                case SyncFieldType.Vector4:
                    string[] split4 = value.Split(new char[] { ',' });
                    GetField<Vector4Field>(fieldID).ExternalSet(new Vector4(float.Parse(split4[0]), float.Parse(split4[1]), float.Parse(split4[2]), float.Parse(split4[3])));
                    break;
                case SyncFieldType.Color:
                    string[] color = value.Split(new char[] { ',' });
                    GetField<ColorField>(fieldID).ExternalSet(new Color(float.Parse(color[0]), float.Parse(color[1]), float.Parse(color[2]), float.Parse(color[3])));
                    break;
                case SyncFieldType.Quaternion:
                    string[] quat = value.Split(new char[] { ',' });
                    GetField<QuaternionField>(fieldID).ExternalSet(new Quaternion(float.Parse(quat[0]), float.Parse(quat[1]), float.Parse(quat[2]), float.Parse(quat[3])));
                    break;
                case SyncFieldType.CompressedQuaternion:
                    string[] compQuat = value.Split(new char[] { ',' });
                    GetField<CompressedQuaternionField>(fieldID).ExternalSet(new Quaternion(float.Parse(compQuat[0]), float.Parse(compQuat[1]), float.Parse(compQuat[2]), float.Parse(compQuat[3])));
                    break;
                case SyncFieldType.String:
                    GetField<StringField>(fieldID).ExternalSet(value);
                    break;                               
            }
        }
        #endregion
    }
}
