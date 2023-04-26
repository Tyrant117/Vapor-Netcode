using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace VaporNetcode
{
    public static partial class ObservableFactory
    {
        public static Dictionary<int, Func<int, ObservableClass>> factoryMap = new();

        public static Dictionary<int, int> counterMap = new(200);
        //Network IDs for Objects
        private static int counter = 0;
        public static int NextUniqueID(int type)
        {
            if (counterMap.TryGetValue(type, out var counter))
            {
                counterMap[type] = counter + 1;
            }
            else
            {
                counterMap[type] = 1;
            }

            int id = counterMap[type];
            if (id == int.MaxValue)
            {
                throw new Exception("connection ID Limit Reached: " + id);
            }

            if (NetLogFilter.logDebug && NetLogFilter.spew) { Debug.LogFormat("Generated Observable ID: {0}", id); }
            return id;
        }

        static ObservableFactory()
        {
            Initialize();
        }

        static partial void Initialize();


#pragma warning disable IDE0051 // Remove unused private members
        private static void AddFactory(int id, Func<int, ObservableClass> factory) => factoryMap[id] = factory;
#pragma warning restore IDE0051 // Remove unused private members

        public static bool TryCreateObservable<T>(int typeId, out T result) where T : ObservableClass
        {
            if (factoryMap.TryGetValue(typeId, out var factory))
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

        public static bool TryCreateObservable<T>(int typeId, int customID, out T result) where T : ObservableClass
        {
            if (factoryMap.TryGetValue(typeId, out var factory))
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
