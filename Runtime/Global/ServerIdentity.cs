using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public class ServerIdentity
    {
        protected bool cleanup;     // Flag that causes the entity to be cleaned from the server when set to true.

        /// <summary>
        /// Called after the entity was updated during this server tick. If the entity is ready to be cleaned from the server it will return true.
        /// </summary>
        /// <returns>True when <see cref="cleanup"/> is true, will remove entity from the server</returns>
        public bool Cleanup => cleanup;

        public bool Active { get; protected set; }
        public bool IsPlayer { get; protected set; }

        // <summary>The set of network connections (players) that can see this object.</summary>
        public Dictionary<ulong, ServerIdentity> observers = new();

        // NetworkIdentities that this connection can see
        public readonly HashSet<ServerIdentity> observing = new();

        public virtual void MarkActive()
        {
            Active = true;
        }

        public virtual void MarkSpawnerActive() { }

        /// <summary>
        /// Called on all entities every server tick.
        /// </summary>
        /// <param name="serverTick"></param>
        public virtual void Tick(long serverTick) { }

        #region - Messages -
        public virtual void AddPacket(CommandMessage msg) { }
        #endregion


        #region - Interest Management -
        public void AddToObserving(ServerIdentity netIdentity)
        {
            if (observing.Add(netIdentity))
            {
                netIdentity.MarkSpawnerActive();
            }
        }

        public void RemoveFromObserving(ServerIdentity netIdentity)
        {
            observing.Remove(netIdentity);
        }        
        #endregion
    }
}
