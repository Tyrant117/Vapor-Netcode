using UnityEngine;

namespace VaporNetcode
{
    public class ClientModule : MonoBehaviour
    {
        /// <summary>
        ///     Called by master server when module should be started
        /// </summary>
        /// <param name="manager"></param>
        public virtual void Initialize()
        {

        }

        /// <summary>
        ///     Called when the manager unloads all the modules.
        /// </summary>
        /// <param name="manager"></param>
        public virtual void Unload()
        {

        }
    }
}