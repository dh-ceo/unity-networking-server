using System;
using System.Collections.Generic;

namespace DevelopersHub.Unity.Networking
{
    internal static class Outgoing
    {

        public enum OutgoingType
        {
            outgoingType = 0
        }

        public static void InitializeClient(int id, string exampleString, int exampleInt)
        {
            Carrier carrier = new Carrier(4);
            carrier.SetInt32((int)OutgoingType.outgoingType);
            #region Set Data
            carrier.SetString(exampleString);
            carrier.SetInt32(exampleInt);
            #endregion
            Configuration.Socket.SendDataTo(id, carrier.values, carrier.space);
            carrier.Dispose();
        }

    }
}