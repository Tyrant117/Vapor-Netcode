using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporMMO.Backend
{
    public struct ServerCharacterDataResult
    {
        public bool foundData;
        public Dictionary<string, Dictionary<string, string>> characterData;

        public void SetupData(string characterId)
        {
            if(characterData == null)
            {
                characterData = new Dictionary<string, Dictionary<string, string>>();
            }

            characterData[characterId] = new();
        }

        public void AddData(string characterId, Dictionary<string, string> kvp)
        {
            characterData[characterId] = kvp;
        }
    }
}
