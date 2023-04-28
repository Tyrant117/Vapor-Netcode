using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporMMO
{
    public enum AuthenticationServiceType : byte
    {
        None,
        Unity,
        Playfab,
        Steam,
        Epic,
        Custom
    }    
}
