using System;
using System.Collections.Generic;

namespace VaporMMO.Backend
{
    [Serializable]
    public struct PlayFabLoginInfoResult
    {
        public bool completeResult;
        public string playfabId;
        public string sessionTicket;
    }

    [Serializable]
    public struct PlayFabCharacterInfo
    {
        public string characterID;
        public string characterName;
        public string characterType;
    }
}
