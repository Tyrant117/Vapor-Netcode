using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using VaporNetcode;

namespace VaporMMO
{
    public class ServerWorldModule : ServerModule
    {
        protected const string TAG = "<color=lightblue><b>[Server World]</b></color>";

        [FoldoutGroup("Interest Management"), SerializeField]
        protected int _navigationLayers = 1;
        [FoldoutGroup("Interest Management"), SerializeField]
        protected int _playerViewRange = 100;
        [FoldoutGroup("Interest Management"), SerializeField]
        protected float _aoiRebuildInterval = 1f;

        [FoldoutGroup("Logs"), SerializeField]
        [InlineProperty, HideLabel]
        protected NetLogger Logger;

        //Network IDs for Objects
        protected uint _idCounter = 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextNetID()
        {
            _idCounter++;
            return _idCounter;
        }

        public readonly Dictionary<int, IServerIdentity> Players = new();
        public readonly Dictionary<uint, IServerIdentity> Entities = new();
        public readonly Dictionary<uint, ITickable> Tickables = new();

        protected InterestManagement aoi;
        protected readonly DictionaryCleanupList<uint, IServerIdentity> _cleanup = new(200);

        public override void Initialize()
        {
            aoi = new InterestManagement(this, _navigationLayers, _playerViewRange, _aoiRebuildInterval);

            UDPServer.RegisterHandler<ClientReadyMessage>(OnHandleReady);
            UDPServer.RegisterHandler<CommandMessage>(OnHandleCommand);
        }

        public override void Update(float deltaTime)
        {
            foreach (var plr in Players.Values)
            {
                if (plr.IsReady)
                {
                    plr.Tick(deltaTime);
                }
            }

            foreach (var tickable in Tickables.Values)
            {
                tickable.Tick(deltaTime);
            }

            aoi.Tick();

            foreach (var entity in Entities.Values)
            {
                if (entity.IsPlayer) { continue; }

                if (entity.Cleanup)
                {
                    ReturnEntity(entity);
                    continue;
                }

                if (entity.Active)
                {
                    entity.Tick(deltaTime);
                }
            }

            foreach (var netID in _cleanup.Content)
            {
                if (Entities.TryGetValue(netID, out var entity))
                {
                    entity.OnCleanup();
                }
            }

            foreach (var entity in Entities.Values)
            {
                entity.CreateInterestPacket();
            }

            foreach (var plr in Players.Values)
            {
                if (plr.IsReady)
                {
                    plr.SendInterestPacket();
                }
            }
            _cleanup.RemoveAll(Entities);
        }

        #region - Creation and Joining -
        public virtual bool TryCreateNewCharacter(string accountID, string characterName, CreateCharacterRequestMessage packet, out AccountDataSpecification result)
        {
            result = new AccountDataSpecification()
            {
                StringID = accountID,
                LastLoggedIn = DateTimeOffset.UtcNow,
                CharacterID = Guid.NewGuid().ToString(),
                CharacterName = characterName,
                CharacterDisplayEquipment = new(),
                ClassData = new(),
                ServerClassData = new(),
                FieldData = new(),
                ServerFieldData = new(),
            };
            return true;
        }

        public virtual JoinWithCharacterResponseMessage Join(INetConnection conn, AccountDataSpecification character)
        {
            return new JoinWithCharacterResponseMessage()
            {
                Scene = string.Empty,
                Status = ResponseStatus.Failed,
            };
        }
        #endregion

        #region - Registration -
        public void RegisterTickable(ITickable tickable)
        {
            if (tickable.IsRegistered) { return; }
            tickable.Register(NextNetID());
            Tickables.Add(tickable.NetID, tickable);
        }

        public void RemoveTickable(uint netID)
        {
            Tickables.Remove(netID);
        }

        public void RegisterEntity(IServerIdentity entity)
        {
            if (entity.IsRegistered) { return; }
            entity.Register(NextNetID());
            Entities.Add(entity.NetID, entity);
        }
        #endregion

        #region - Messages -
        private void OnHandleReady(INetConnection conn, ClientReadyMessage msg)
        {
            if (Players.ContainsKey(conn.ConnectionID))
            {
                ((Peer)conn).IsReady = true;
            }
            else
            {
                conn.Disconnect();
            }
        }

        private void OnHandleCommand(INetConnection conn, CommandMessage msg)
        {
            if (Players.TryGetValue(conn.ConnectionID, out var identity))
            {
                identity.AddPacket(msg);
            }
            else
            {
                conn.Disconnect();
            }
        }
        #endregion

        #region - Pools -
        public virtual void ReturnEntity(IServerIdentity entity)
        {
            _cleanup.Add(entity.NetID);
        }
        #endregion
    }
}
