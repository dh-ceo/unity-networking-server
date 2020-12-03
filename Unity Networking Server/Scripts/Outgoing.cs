using System;
using System.Collections.Generic;

namespace DevelopersHub.Unity.Networking
{
    internal static class Outgoing
    {

        public enum OutgoingType
        {
            connected = 0
        }

        public static void Connected(int id)
        {
            Carrier carrier = new Carrier(4);
            carrier.SetInt32((int)OutgoingType.connected);
            Configuration.Socket.SendDataTo(id, carrier.values, carrier.space);
            carrier.Dispose();
        }

    }
}