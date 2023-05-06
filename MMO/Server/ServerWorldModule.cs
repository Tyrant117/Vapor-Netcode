using System;
using System.Collections.Generic;
using VaporNetcode;

namespace VaporMMO
{
    public class ServerWorldModule : ServerModule
    {
        public readonly Dictionary<int, ServerIdentity> Players = new();

        public override void Initialize()
        {
            UDPServer.RegisterHandler<CommandMessage>(OnHandleCommand);
        }

        public override void Update()
        {
            foreach (var plr in Players.Values)
            {
                plr.SendInterestPacket();
            }
        }

        public virtual AccountDataSpecification CreateNewCharacter(string accountID, string characterName, byte gender)
        {
            var spec = new AccountDataSpecification()
            {
                StringID = accountID,
                CharacterName = characterName,
                LastLoggedIn = DateTimeOffset.UtcNow,
                CharacterDisplayEquipment = new(),
                ClassData = new(),
                FieldData = new(),
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
    }
}
