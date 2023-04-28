using VaporNetcode;

namespace VaporMMO
{
    [System.Serializable]
    public struct PlayFabAuthenticationPacket
    {
        public string playfabId;
        public string sessionToken;

        public void Write(NetworkWriter w)
        {
            w.WriteString(playfabId);
            w.WriteString(sessionToken);
        }
    }
}
