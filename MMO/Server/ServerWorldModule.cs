using VaporNetcode;

namespace VaporMMO
{
    public class ServerWorldModule : ServerModule
    {
        public override void Initialize()
        {

        }

        public virtual JoinWithCharacterResponseMessage Join(INetConnection conn, AccountDataSpecification character, ushort responseID)
        {
            return new JoinWithCharacterResponseMessage()
            {
                Scene = string.Empty,
                ResponseID = responseID,
                Status = ResponseStatus.Failed,
            };
        }
    }
}
