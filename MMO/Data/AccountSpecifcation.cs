using System;
using UnityEngine;
using VaporNetcode;

namespace VaporMMO
{
    [Serializable]
    public struct AccountSpecifcation
    {
        /// <summary>
        /// StringID of the account owner. (Playfab or any service using strings. is also the steamid in string form if using steamworks.)
        /// </summary>
        public string StringID;
        /// <summary>
        /// SteamID of the account owner.
        /// </summary>
        public ulong LinkedSteamID;
        /// <summary>
        /// The EOS_ProductUserID when using epic relay services.
        /// </summary>
        public string LinkedEpicProductUserID;
        /// <summary>
        /// Email tied to this account.
        /// </summary>
        public string Email;
        /// <summary>
        /// Hashed and salted password string.
        /// </summary>
        public string Password;
        /// <summary>
        /// Hashed and salted password string.
        /// </summary>
        public string Salt;
        /// <summary>
        /// Last time this account logged in.
        /// </summary>
        public DateTimeOffset? LastLoggedIn;
        /// <summary>
        /// The permissions level of the account.
        /// </summary>
        public PermisisonLevel Permissions;
        /// <summary>
        /// The end of the ban if this account has been banned.
        /// </summary>
        public DateTimeOffset? EndOfBan;

        public static AccountSpecifcation FromJson(string json)
        {
            return JsonUtility.FromJson<AccountSpecifcation>(json);
        }
    }
}
