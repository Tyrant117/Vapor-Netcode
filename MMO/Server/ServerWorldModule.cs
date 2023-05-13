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
        public const string TAG = "<color=cyan><b>[Server World]</b></color>";
        public const string WARNING = "<color=yellow><b>[!]</b></color>";

        [FoldoutGroup("Interest Management"), SerializeField]
        private int _navigationLayers = 1;
        [FoldoutGroup("Interest Management"), SerializeField]
        private int _playerViewRange = 100;
        [FoldoutGroup("Interest Management"), SerializeField]
        private float _aoiRebuildInterval = 1f;

        //Network IDs for Objects
        private uint _idCounter = 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextNetID()
        {
            _idCounter++;
            if (NetLogFilter.logInfo && NetLogFilter.spew) { Debug.Log($"{TAG} Generated ID: {0}"); }
            return _idCounter;
        }

        public readonly Dictionary<int, IServerIdentity> Players = new();
        public readonly Dictionary<uint, IServerIdentity> Entities = new();
        public readonly Dictionary<uint, ITickable> Tickables = new();

        private InterestManagement aoi;
        private readonly DictionaryCleanupList<uint, IServerIdentity> _cleanup = new(200);

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
        public virtual AccountDataSpecification CreateNewCharacter(string accountID, string characterName, byte gender)
        {
            var spec = new AccountDataSpecification()
            {
                StringID = accountID,
                CharacterName = characterName,
                LastLoggedIn = DateTimeOffset.UtcNow,
                CharacterDisplayEquipment = new(),
                ClassData = new(),
                ServerClassData = new(),
                FieldData = new(),
                ServerFieldData = new(),                 
            };
            return spec;
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
