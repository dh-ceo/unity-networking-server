using System;
using System.Threading;

namespace DevelopersHub.Unity.Networking
{
    class Program
    {

        private static Thread thread;

        static void Main()
        {
            thread = new Thread(new ThreadStart(ServerThread));
            thread.Start();
            bool database = Database.CheckConnection();
            if (!database)
            {
                Console.WriteLine("Failed to connect the database.");
                return;
            }
            Configuration.StartNetwork();
            Configuration.Socket.StartListening(5555, 5, 1);
            Console.WriteLine("Network initialized successfully.");
        }

        private static void ServerThread()
        {
            while (true)
            {
                Database.CheckTasks();
            }
        }

    }
}