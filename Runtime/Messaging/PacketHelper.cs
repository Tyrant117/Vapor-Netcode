using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace VaporNetcode
{
    public static class PacketID<T> where T : struct, ISerializablePacket
    {
        // automated message id from type hash.
        // platform independent via stable hashcode.
        // => convenient so we don't need to track messageIds across projects
        // => addons can work with each other without knowing their ids before
        // => 2 bytes is enough to avoid collisions.
        //    registering a messageId twice will log a warning anyway.
        public static readonly ushort ID = (ushort)(typeof(T).FullName.GetStableHashCode());
    }

    public static class PacketHelper
    {
        private static Dictionary<ushort, Func<NetworkReader, ISerializablePacket>> _packets = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _packets.Clear();
        }

        /// <summary>
        ///     Deserialized data into the provided packet and returns it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public static T Deserialize<T>(NetworkReader r) where T : struct, ISerializablePacket
        {
            if (!_packets.TryGetValue(PacketID<T>.ID, out var activator))
            {
                ConstructorInfo ctor = typeof(T).GetConstructors()[0];
                ParameterExpression param = Expression.Parameter(typeof(NetworkReader));
                NewExpression newExp = Expression.New(ctor, param);
                activator = Expression.Lambda<Func<NetworkReader, ISerializablePacket>>(newExp, param).Compile();
                _packets[PacketID<T>.ID] = activator;
            }
            return (T)activator(r);
        }
    }
}