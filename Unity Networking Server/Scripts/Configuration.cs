using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevelopersHub.Unity.Networking
{
    internal static class Configuration
    {

        private static Core core;
        internal static Core Socket
        {
            get { return core; }
            set
            {
                if (core != null)
                {
                    core.ConnectionReceived -= OnConnectionReceived;
                    core.ConnectionLost -= OnConnectionLost;
                }
                core = value;
                if (core != null)
                {
                    core.ConnectionReceived += OnConnectionReceived;
                    core.ConnectionLost += OnConnectionLost;
                }
            }
        }

        internal static void StartNetwork()
        {
            if (core != null)
            {
                return;
            }
            Socket = new Core(100) { BufferLimit = 2048000, PacketAcceptLimit = 100, PacketDisconnectCount = 200 };
            Incoming.PacketRouter();
        }

        internal static void OnConnectionReceived(int id)
        {
            Database.ClientConnected(id);
        }

        internal static void OnConnectionLost(int id)
        {
            Database.ClientDisconnected(id);
        }

    }
}