using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public static class SyncFieldFactory
    {
        private static readonly Dictionary<int, Func<int, SyncClass>> _factoryMap = new();

        private static readonly Dictionary<int, int> _counterMap = new(100);
        private static int NextUniqueID(int type)
        {
            _counterMap[type] = _counterMap.TryGetValue(type, out var counter) ? counter + 1 : 1;

            int id = _counterMap[type];
            if (id == int.MaxValue)
            {
                throw new Exception("ID Limit Reached: " + id);
            }

            if (NetLogFilter.logDebug && NetLogFilter.spew) { Debug.LogFormat("Generated Observable ID: {0}", id); }
            return id;
        }

        static SyncFieldFactory()
        {

        }

#pragma warning disable IDE0051 // Remove unused private members
        public static void AddFactory(int id, Func<int, SyncClass> factory) => _factoryMap[id] = factory;
#pragma warning restore IDE0051 // Remove unused private members

        public static bool TryCreateSyncClass<T>(int typeId, out T result) where T : SyncClass
        {
            if (_factoryMap.TryGetValue(typeId, out var factory))
            {
                result = factory.Invoke(NextUniqueID(typeId)) as T;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public static bool TryCreateSyncClass<T>(int typeId, int customID, out T result) where T : SyncClass
        {
            if (_factoryMap.TryGetValue(typeId, out var factory))
            {
                result = factory.Invoke(customID) as T;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
    }
}
