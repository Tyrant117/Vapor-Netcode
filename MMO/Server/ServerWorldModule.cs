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

        public virtual AccountDataSpecification CreateNewCharacter(string characterName, byte gender)
        {
            var spec = new AccountDataSpecification()
            {
                CharacterName = characterName,
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
