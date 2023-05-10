using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public interface IServerIdentity
    {
        /// <summary>
        /// Called after the entity was updated during this server tick. If the entity is ready to be cleaned from the server it will return true.
        /// </summary>
        /// <returns>True when <see cref="cleanup"/> is true, will remove entity from the server</returns>
        bool Cleanup { get; }

        bool IsRegistered { get; set; }
        uint NetID { get; set; }
        Peer Peer { get; }
        bool IsPeer => Peer != null;
        bool IsReady { get; }
        bool Active { get; set; }
        bool IsPlayer { get; }

        int NavigationLayer { get; set; }
        Vector3 Position { get; }

        EntityInterestPacket CurrentInterestPacket { get; }
        // <summary>The set of network connections (players) that can see this object.</summary>
        Dictionary<int, IServerIdentity> Observers { get; set; }

        // NetworkIdentities that this connection can see
        HashSet<IServerIdentity> Observing { get; }
        

        public void Register(uint netID)
        {
            NetID = netID;
            IsRegistered = true;
        }

        public void MarkActive()
        {
            Active = true;
        }

        public void MarkSpawnerActive();

        /// <summary>
        /// Called on all entities every server tick.
        /// </summary>
        /// <param name="serverTick"></param>
        public void Tick();

        #region - Messages -
        public void AddPacket(CommandMessage msg);

        public void CreateInterestPacket();

        ArraySegment<byte> CreateFullInterestPacket(NetworkWriter w);

        public void SendInterestPacket();
        #endregion

        #region - Interest Management -
        public void AddToObserving(IServerIdentity netIdentity)
        {
            if (Observing.Add(netIdentity))
            {
                netIdentity.MarkSpawnerActive();
            }
        }

        public void RemoveFromObserving(IServerIdentity netIdentity)
        {
            Observing.Remove(netIdentity);
        }        
        #endregion
    }
}
