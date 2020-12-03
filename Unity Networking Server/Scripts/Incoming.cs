using System;
using System.Collections.Generic;

namespace DevelopersHub.Unity.Networking
{
    internal static class Incoming
    {

        public enum IncomingType
        {
            incomingType = 0
        }

        internal static void PacketRouter()
        {
            Configuration.Socket.PacketId[(int)IncomingType.incomingType] = ExampleDataReceive;
        }

        private static void ExampleDataReceive(int id, ref byte[] data)
        {
            Carrier carrier = new Carrier(data);

            #region Get Data
            string stringExample = carrier.GetString();
            int intExample = carrier.GetInt32();
            bool boolExample = carrier.GetBoolean();
            // ...
            #endregion

            #region Process Data
            object[] args = new object[] { id, stringExample, intExample, boolExample };
            Database.AddMethod("MethodName", args);
            #endregion

            carrier.Dispose();
        }

    }
}