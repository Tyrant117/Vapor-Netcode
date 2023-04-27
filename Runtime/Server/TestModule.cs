using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    [System.Serializable]
    public class TestModule : ServerModule
    {
        public int someNumber;

        public override void Initialize()
        {
            throw new System.NotImplementedException();
        }

        public override void Unload()
        {
            throw new System.NotImplementedException();
        }

        public override void Update()
        {
            throw new System.NotImplementedException();
        }
    }
}
