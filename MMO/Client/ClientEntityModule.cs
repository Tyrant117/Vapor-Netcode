using Sirenix.OdinInspector;
using System;
using UnityEngine;
using VaporNetcode;

namespace VaporMMO
{
    public class ClientEntityModule : ClientModule
    {
        protected const string TAG = "<color=lightblue><b>[Client Entity]</b></color>";

        [FoldoutGroup("Logs"), SerializeField]
        [InlineProperty, HideLabel]
        protected NetLogger Logger;

        public event Action<EntityInterestPacket> UpdatePlayer;
        public event Action<EntityInterestPacket> UpdateCreature;
        public event Action<EntityInterestPacket> UpdateInteractable;

        public override void Initialize()
        {
            UDPClient.RegisterHandler<SyncDataMessage>(OnProfileUpdate);
            UDPClient.RegisterHandler<InterestMessage>(OnInterestUpdate);
            UDPClient.RegisterHandler<LostInterestMessage>(OnLostInterestUpdate);
        }

        private void OnProfileUpdate(INetConnection conn, SyncDataMessage msg)
        {
            if (conn is Peer peer && peer.IsReady)
            {
                peer.SyncBatcher.Unbatch(msg);
            }
        }

        private void OnInterestUpdate(INetConnection conn, InterestMessage msg)
        {
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
                    case 4:
                        OnUpdateLoot(entity);
                        break;

                }
            }
        }

        private void OnLostInterestUpdate(INetConnection conn, LostInterestMessage msg)
        {
            if (!conn.IsReady) { return; }

            foreach (var entity in msg.Packets)
            {
                switch (entity.InterestType)
                {
                    case 1:
                        if (entity.NetID == conn.NetID) { continue; } // Don't care about yourself.
                        OnLostPlayer(entity);
                        break;
                    case 2:
                        OnLostCreature(entity);
                        break;
                    case 3:
                        OnLostInteractable(entity);
                        break;
                    case 4:
                        OnLostLoot(entity);
                        break;

                }
            }
        }

        protected virtual void OnUpdatePlayer(EntityInterestPacket player)
        {

        }

        protected virtual void OnLostPlayer(EntityLostInterestPacket player)
        {

        }

        protected virtual void OnUpdateCreature(EntityInterestPacket creature)
        {

        }

        protected virtual void OnLostCreature(EntityLostInterestPacket creature)
        {

        }

        protected virtual void OnUpdateInteractable(EntityInterestPacket interactable)
        {

        }

        protected virtual void OnLostInteractable(EntityLostInterestPacket interactable)
        {

        }

        protected virtual void OnUpdateLoot(EntityInterestPacket interactable)
        {

        }

        protected virtual void OnLostLoot(EntityLostInterestPacket interactable)
        {

        }
    }
}
