using VaporNetcode;

namespace VaporMMO
{
    public struct JoinWithCharacterRequestMessage : INetMessage
    {
        public string CharacterName;

        public JoinWithCharacterRequestMessage(NetworkReader r)
        {
            CharacterName = r.ReadString();
        }


        public void Serialize(NetworkWriter w)
        {
            w.WriteString(CharacterName);
        }
    }
}
