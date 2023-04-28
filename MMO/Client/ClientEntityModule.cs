using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
                        UpdatePlayer.Invoke(entity);
                        break;
                    case 2:
                        UpdateCreature.Invoke(entity);
                        break;
                    case 3:
                        UpdateInteractable.Invoke(entity);
                        break;

                }
            }
        }
    }
}
