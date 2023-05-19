using System;
using UnityEngine;
using VaporNetcode;

namespace VaporMMO
{
    public class ClientEntityModule : ClientModule
    {
        private const string TAG = "<color=lightblue><b>[Client Entity]</b></color>";

        public event Action<EntityInterestPacket> UpdatePlayer;
        public event Action<EntityInterestPacket> UpdateCreature;
        public event Action<EntityInterestPacket> UpdateInteractable;

        public override void Initialize()
        {
            UDPClient.RegisterHandler<SyncDataMessage>(OnProfileUpdate);
            UDPClient.RegisterHandler<InterestMessage>(OnInterestUpdate);
        }

        private void OnProfileUpdate(INetConnection conn, SyncDataMessage msg)
        {
            if (NetLogFilter.LogInfo && NetLogFilter.spew)
            {
                Debug.Log($"{TAG} Profile Updated");
            }
            if (conn is Peer peer && peer.IsReady)
            {
                peer.SyncBatcher.Unbatch(msg);
            }
        }

        private void OnInterestUpdate(INetConnection conn, InterestMessage msg)
        {
            if (NetLogFilter.LogInfo && NetLogFilter.spew)
            {
                Debug.Log($"{TAG} Interest Updated");
            }

            if (!conn.IsReady) { return; }

            foreach (var entity in msg.packets)
            {
                switch (entity.InterestType)
                {
                    case 1:
                        if (entity.NetID == conn.NetID) { continue; } // Don't care about yourself.
                        OnUpdatePlayer(entity);
                        break;
                    case 2:
                        OnUpdateCreature(entity);
                        break;
                    case 3:
                        OnUpdateInteractable(entity);
                        break;

                }
            }
        }

        protected virtual void OnUpdatePlayer(EntityInterestPacket player)
        {

        }

        protected virtual void OnUpdateCreature(EntityInterestPacket creature)
        {

        }

        protected virtual void OnUpdateInteractable(EntityInterestPacket interactable)
        {

        }
    }
}
