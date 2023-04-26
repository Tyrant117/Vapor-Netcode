using System;

namespace VaporNetcode
{
    public interface INetConnection
    {
        bool IsConnected { get; set; }
        bool IsAuthenticated { get; set; }
        int ConnectionID { get; }
        ulong GenericULongID { get; }
        string GenericStringID { get; set; }
        int SpamCount { get; set; }

        void Disconnect(int reason = 0);

        //bool SendMessage(ArraySegment<byte> segment, int clientConnectionID = 0);
        //bool SendSimulatedMessage(ArraySegment<byte> segment, int clientConnectionID = 0);
        //void Authenticated();
    }
}
