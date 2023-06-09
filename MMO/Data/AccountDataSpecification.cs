using System;
using System.Collections.Generic;
using UnityEngine;
using VaporNetcode;
using VaporObservables;

namespace VaporMMO
{
    [Serializable]
    public struct AccountDataSpecification
    {
        /// <summary>
        /// The stringID of the account that owns this character.
        /// </summary>
        public string StringID;
        /// <summary>
        /// The steamID of the steam account that owns this character.
        /// </summary>
        public ulong LinkedSteamID;
        /// <summary>
        /// The last time this character logged in.
        /// </summary>
        public DateTimeOffset? LastLoggedIn;
        /// <summary>
        /// The internal ID that belongs to this character.
        /// </summary>
        public string CharacterID;
        /// <summary>
        /// The internal ID of the server this character is on.
        /// </summary>
        public string ServerID;
        /// <summary>
        /// The display name of the character.
        /// </summary>
        public string CharacterName;
        /// <summary>
        /// The level of the character
        /// </summary>
        public string CharacterLevel;
        /// <summary>
        /// The class of the character
        /// </summary>
        public string CharacterClass;
        /// <summary>
        /// The ids of the equipment the character is currently wearing.
        /// </summary>
        public List<int> CharacterDisplayEquipment;
        /// <summary>
        /// All the data that this character has saved that is class based (items, stats, buffs, etc.)
        /// </summary>
        public List<SavedSyncClass> ClassData;
        /// <summary>
        /// All the data that this character has saved that is field based. That only need to exist on the server.
        /// </summary>
        public List<SavedObservableClass> ServerClassData;
        /// <summary>
        /// All the data that this character has saved that is field based. That needs to get synced back to the player. (logout locations, display name, etc)
        /// </summary>
        public List<SavedSyncField> FieldData;
        /// <summary>
        /// All the data that this character has saved that is field based. That only need to exist on the server.
        /// </summary>
        public List<SavedObservableField> ServerFieldData;

        public static AccountDataSpecification FromJson(string json)
        {
            return JsonUtility.FromJson<AccountDataSpecification>(json);
        }
    }
}
