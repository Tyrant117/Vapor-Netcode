using System;
using VaporNetcode;

namespace VaporMMO
{
    public class ClientEntityModule : ClientModule
    {
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
            ((Peer)conn).SyncBatcher.Unbatch(msg);
        }

        private void OnInterestUpdate(INetConnection conn, InterestMessage msg)
        {
            foreach (var entity in msg.packets)
            {
                switch (entity.InterestType)
                {
                    case 1:
                        if (entity.ConnectionID == conn.ConnectionID) { continue; } // Don't care about yourself.
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
