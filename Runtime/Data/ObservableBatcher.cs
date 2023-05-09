using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public class ObservableBatcher
    {
        public bool IsDirty { get; private set; }

        public Dictionary<Vector2Int, ObservableClass> classMap = new(50);
        public Dictionary<int, ObservableField> fieldMap = new(50);

        public Dictionary<Vector2Int, ObservableClass> dirtyClasses = new(50);
        public Dictionary<int, ObservableField> dirtyFields = new(50);

        public event Action<ObservableClass> ClassCreated;
        public event Action<ObservableField> FieldCreated;
        public event Action<int, int> FirstUnbatch;
        public event Action<int, int> Unbatched;

        private bool isFirstUnbatch = true;
        private readonly NetworkWriter w;

        public ObservableBatcher()
        {
            isFirstUnbatch = true;
            w = new();
        }

        #region - Getters -
        public bool TryGetClass<T>(int uniqueID, out T sync) where T : ObservableClass
        {
            var key = new Vector2Int(ObservableClassID<T>.ID, uniqueID);
            if (classMap.TryGetValue(key, out var @class))
            {
                sync = @class as T;
                return true;
            }
            else
            {
                if (NetLogFilter.logError)
                {
                    Debug.Log($"{nameof(T)} not found.");
                }
                sync = default;
                return false;
            }
        }

        public bool TryGetField<T>(int fieldKey, out T sync) where T : ObservableField
        {
            if (fieldMap.TryGetValue(fieldKey, out var field))
            {
                sync = field as T;
                return true;
            }
            else
            {
                if (NetLogFilter.logError)
                {
                    Debug.Log($"{nameof(T)} not found.");
                }
                sync = default;
                return false;
            }
        }
        #endregion

        #region - Registration -
        public void RegisterObserableClass(ObservableClass observableClass)
        {
            Vector2Int key = new(observableClass.Type, observableClass.ID);
            if (classMap.ContainsKey(key))
            {
                if (NetLogFilter.logInfo && NetLogFilter.syncVars) { Debug.Log($"Overwriting ObservableClass at Key: {key}"); }
            }

            if (!dirtyClasses.ContainsKey(key))
            {
                dirtyClasses.Add(key, observableClass);
            }
            classMap[key] = observableClass;
            observableClass.Dirtied += ObservableClass_Dirtied;
            IsDirty = true;
        }

        public void RegisterObservableField(ObservableField observableField)
        {
            int key = observableField.FieldID;
            if (fieldMap.ContainsKey(key))
            {
                if (NetLogFilter.logInfo && NetLogFilter.syncVars) { Debug.Log($"Overwriting ObservableField at Key: {key}"); }
            }

            if (!dirtyFields.ContainsKey(key))
            {
                dirtyFields.Add(key, observableField);
            }

            fieldMap[key] = observableField;
            observableField.Dirtied += ObservableField_Dirtied;
            IsDirty = true;
        }
        #endregion

        #region - Dirty -
        private void ObservableClass_Dirtied(ObservableClass observableClass)
        {
            Vector2Int key = new(observableClass.Type, observableClass.ID);
            if (!dirtyClasses.ContainsKey(key))
            {
                dirtyClasses.Add(key, observableClass);
            }
            IsDirty = true;
        }

        private void ObservableField_Dirtied(ObservableField observableField)
        {
            if (!dirtyFields.ContainsKey(observableField.FieldID))
            {
                dirtyFields.Add(observableField.FieldID, observableField);
            }
            IsDirty = true;
        }
        #endregion

        #region - Batching -
        public SyncDataMessage Batch(bool full)
        {
            w.Reset();
            if (full)
            {
                _Full();
            }
            else
            {
                _Partial();
                dirtyClasses.Clear();
                dirtyFields.Clear();
                IsDirty = false;
            }


            var packet = new SyncDataMessage
            {
                data = w.ToArraySegment()
            };

            return packet;

            void _Full()
            {
                w.WriteInt(classMap.Count);                
                foreach (var oc in classMap.Values)
                {
                    oc.SerializeInFull(w);
                }
                w.WriteInt(fieldMap.Count);
                foreach (var of in fieldMap.Values)
                {
                    of.SerializeInFull(w);
                }

                if (NetLogFilter.logDebug && NetLogFilter.syncVars && NetLogFilter.spew)
                {
                    Debug.Log($"Fully Batching | Classes: {classMap.Count} Fields: {fieldMap.Count}");
                }
            }

            void _Partial()
            {
                w.WriteInt(dirtyClasses.Count);
                foreach (var oc in dirtyClasses.Values)
                {
                    oc.Serialize(w);
                }

                w.WriteInt(dirtyFields.Count);
                foreach (var of in dirtyFields.Values)
                {
                    of.Serialize(w);
                }
            }
        }

        public void Unbatch(SyncDataMessage packet)
        {
            using var r = NetworkReaderPool.Get(packet.data);
            int classCount = r.ReadInt();
            if (NetLogFilter.logDebug && NetLogFilter.syncVars && NetLogFilter.spew)
            {
                Debug.Log($"Unbatching Classes: {classCount}");
            }
            for (int i = 0; i < classCount; i++)
            {
                ObservableClass.StartDeserialize(r, out int type, out int id);
                var key = new Vector2Int(type, id);
                if (classMap.TryGetValue(key, out var @class))
                {
                    @class.Deserialize(r);
                }
                else
                {
                    if (ObservableFactory.TryCreateObservable(type, id, out ObservableClass newClass))
                    {
                        classMap[key] = newClass;
                        if (NetLogFilter.logDebug && NetLogFilter.syncVars) { Debug.Log($"Unbatch and Create Class | {newClass.GetType().Name} [{id}]"); }
                        newClass.Deserialize(r);
                        ClassCreated?.Invoke(newClass);
                    }
                }
            }

            int fieldCount = r.ReadInt();
            if (NetLogFilter.logDebug && NetLogFilter.syncVars && NetLogFilter.spew)
            {
                Debug.Log($"Unbatching Fields: {fieldCount}");
            }
            for (int i = 0; i < fieldCount; i++)
            {
                ObservableField.StartDeserialize(r, out int id, out var type);
                if (fieldMap.TryGetValue(id, out var field))
                {
                    field.Deserialize(r);
                }
                else
                {
                    var newField = ObservableField.GetFieldByType(id, type, false, false);
                    fieldMap[id] = newField;
                    if (NetLogFilter.logDebug && NetLogFilter.syncVars) { Debug.Log($"Unbatch and Create Field | {type} [{id}]"); }
                    newField.Deserialize(r);
                    FieldCreated?.Invoke(newField);
                }
            }
            if (isFirstUnbatch)
            {
                isFirstUnbatch = false;
                FirstUnbatch?.Invoke(classCount, fieldCount);
            }
            else
            {
                Unbatched?.Invoke(classCount, fieldCount);
            }
        }
        #endregion

        #region - Saving and Loading -
        public void Save(out List<SavedObservableClass> classes, out List<SavedObservable> fields)
        {
            classes = new(classMap.Values.Count);
            fields = new(fieldMap.Values.Count);

            foreach (var @class in classMap.Values)
            {
                classes.Add(@class.Save());
            }

            foreach (var field in fieldMap.Values)
            {
                if (field.SaveValue)
                {
                    fields.Add(field.Save());
                }
            }
        }

        public void Load(List<SavedObservableClass> classes, List<SavedObservable> fields)
        {
            foreach (var @class in classes)
            {
                Vector2Int key = new(@class.Type, @class.ID);
                if (classMap.TryGetValue(key, out var observable))
                {
                    observable.Load(@class);
                }
            }

            foreach (var field in fields)
            {
                if (fieldMap.TryGetValue(field.ID, out var observable))
                {
                    observable.Load(field);
                }
            }
        }
        #endregion
    }
}
