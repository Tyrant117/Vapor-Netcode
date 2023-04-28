using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public interface IResponsePacket
    {
        /// <summary>
        ///     ID of the response callback.
        /// </summary>
        ushort ResponseID { get; set; }

        /// <summary>
        ///     Message status code
        /// </summary>
        ResponseStatus Status { get; set; }
    }
}
