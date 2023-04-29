using System;
using System.Collections.Generic;
using VaporNetcode;

namespace VaporMMO
{
    public struct GetAccountDataRequestMessage : INetMessage
    {
        public GetAccountDataRequestMessage(NetworkReader r)
        {

        }

        public void Serialize(NetworkWriter w)
        {

        }
    }

    public struct GetAccountDataResponseMessage : IResponseMessage
    {
        public enum InitializationResult : byte
        {
            NoAccountFound = 0,
            NoCharactersFound = 1,
            AccountWithCharactersFound = 2,
        }

        public struct FormattedData
        {
            public string serverName;
            public string characterName;
            public string characterClass;
            public string characterLevel;
            public List<int> equipment;

            public void Write(NetworkWriter w)
            {
                w.WriteString(serverName);
                w.WriteString(characterName);
                w.WriteString(characterClass);
                w.WriteString(characterLevel);
                w.WriteInt(equipment.Count);
                foreach (var eqp in equipment)
                {
                    w.WriteInt(eqp);
                }
            }

            public static FormattedData Read(NetworkReader r)
            {
                string serverName = r.ReadString();
                string cName = r.ReadString();
                string cClass = r.ReadString();
                string cLevel = r.ReadString();
                int eqpCount = r.ReadInt();
                List<int> cEqp = new(eqpCount);
                for (int i = 0; i < eqpCount; i++)
                {
                    cEqp.Add(r.ReadInt());
                }

                return new FormattedData()
                {
                    serverName = serverName,
                    characterName = cName,
                    characterClass = cClass,
                    characterLevel = cLevel,
                    equipment = cEqp
                };
            }
        }

        public InitializationResult result;
        /// <summary>
        /// The permissions level of the account.
        /// </summary>
        public PermisisonLevel permissions;
        /// <summary>
        /// The end of the ban if this account has been banned.
        /// </summary>
        public DateTimeOffset endOfBan;
        /// <summary>
        /// The list of characters that belong to the account
        /// </summary>
        public List<FormattedData> characters;

        public ResponseStatus Status { get; set; }

        public void FormatAccountData(List<AccountDataSpecification> ads)
        {
            characters = new();
            foreach (var data in ads)
            {
                characters.Add(new FormattedData()
                {
                    serverName = data.ServerID,
                    characterName = data.CharacterName,
                    characterClass = data.CharacterClass,
                    characterLevel = data.CharacterLevel,
                    equipment = data.CharacterDisplayEquipment
                });
            }
        }

        public GetAccountDataResponseMessage(NetworkReader r)
        {
            result = (InitializationResult)r.ReadByte();
            permissions = (PermisisonLevel)r.ReadInt();
            endOfBan = permissions == PermisisonLevel.Banned ? DateTimeOffset.FromUnixTimeSeconds(r.ReadLong()) : default;
            characters = new();
            int characterCount = r.ReadInt();
            for (int i = 0; i < characterCount; i++)
            {
                characters.Add(FormattedData.Read(r));
            }

            Status = (ResponseStatus)r.ReadByte();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteByte((byte)result);
            w.WriteInt((int)permissions);
            if(permissions == PermisisonLevel.Banned)
            {
                w.WriteLong(endOfBan.ToUnixTimeSeconds());
            }
            w.WriteInt(characters.Count);
            for (int i = 0; i < characters.Count; i++)
            {
                characters[i].Write(w);
            }

            w.WriteByte((byte)Status);
        }
    }
}
