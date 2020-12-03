using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace DevelopersHub.Unity.Networking
{
    public sealed class Core : IDisposable
    {

        private Dictionary<int, Socket> socket;
        private List<int> unsignedIndex;
        private Socket listener;
        private int packetCount;
        public DataArgs[] PacketId;

        public int BufferLimit { get; set; }

        public int ClientLimit { get; }

        public bool IsListening { get; private set; }

        public int HighIndex { get; private set; }

        public int PacketAcceptLimit { get; set; }

        public int PacketDisconnectCount { get; set; }

        public event Core.ConnectionArgs ConnectionReceived;

        public event Core.ConnectionArgs ConnectionLost;

        public event Core.CrashReportArgs CrashReport;

        public event Core.PacketInfoArgs PacketReceived;

        public event Core.TrafficInfoArgs TrafficReceived;

        public Core(int packetCount, int clientLimit = 0)
        {
            if (listener != null || socket != null)
            {
                return;
            }
            socket = new Dictionary<int, Socket>();
            unsignedIndex = new List<int>();
            ClientLimit = clientLimit;
            this.packetCount = packetCount;
            PacketId = new Core.DataArgs[packetCount];
        }

        public void StartListening(int port, int backlog, int startIndex)
        {
            if (socket == null || IsListening || listener != null)
            {
                return;
            }
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind((EndPoint)new IPEndPoint(IPAddress.Any, port));
            IsListening = true;
            listener.Listen(backlog);
            listener.BeginAccept(new AsyncCallback(DoAcceptClient), (object)startIndex);
        }

        public void StopListening()
        {
            if (!IsListening || socket == null)
            {
                return;
            }
            IsListening = false;
            if (listener == null)
            {
                return;
            }
            listener.Close();
            listener.Dispose();
            listener = (Socket)null;
        }

        private void DoAcceptClient(IAsyncResult ar)
        {
            Socket socket = listener.EndAccept(ar);
            int asyncState = (int)ar.AsyncState;
            int emptySlot = FindEmptySlot(asyncState);
            if (ClientLimit > 0 && emptySlot > ClientLimit)
            {
                socket.Disconnect(false);
                socket.Dispose();
                socket = (Socket)null;
            }
            this.socket.Add(emptySlot, socket);
            this.socket[emptySlot].ReceiveBufferSize = 8192;
            this.socket[emptySlot].SendBufferSize = 8192;
            BeginReceiveData(emptySlot);
            Core.ConnectionArgs connectionReceived = ConnectionReceived;
            if (connectionReceived != null)
            {
                connectionReceived(emptySlot);
            }
            if (!IsListening)
            {
                return;
            }
            listener.BeginAccept(new AsyncCallback(DoAcceptClient), (object)asyncState);
        }

        private void BeginReceiveData(int index)
        {
            Core.ReceiveState receiveState = new Core.ReceiveState(index);
            socket[index].BeginReceive(receiveState.Buffer, 0, 8192, SocketFlags.None, new AsyncCallback(DoReceive), (object)receiveState);
        }

        private void DoReceive(IAsyncResult ar)
        {
            Core.ReceiveState asyncState = (Core.ReceiveState)ar.AsyncState;
            int length1;
            try
            {
                length1 = socket[asyncState.Index].EndReceive(ar);
            }
            catch
            {
                Core.CrashReportArgs crashReport = CrashReport;
                if (crashReport != null)
                {
                    crashReport(asyncState.Index, "ConnectionForciblyClosedException");
                }
                Disconnect(asyncState.Index);
                asyncState.Dispose();
                return;
            }
            if (length1 < 1)
            {
                if (!socket.ContainsKey(asyncState.Index))
                {
                    asyncState.Dispose();
                }
                else if (socket[asyncState.Index] == null)
                {
                    asyncState.Dispose();
                }
                else
                {
                    Core.CrashReportArgs crashReport = CrashReport;
                    if (crashReport != null)
                    {
                        crashReport(asyncState.Index, "BufferUnderflowException");
                    }
                    Disconnect(asyncState.Index);
                    asyncState.Dispose();
                }
            }
            else
            {
                Core.TrafficInfoArgs trafficReceived = TrafficReceived;
                if (trafficReceived != null)
                {
                    trafficReceived(length1, ref asyncState.Buffer);
                }
                ++asyncState.PacketCount;
                if (PacketDisconnectCount > 0 && asyncState.PacketCount >= PacketDisconnectCount)
                {
                    Core.CrashReportArgs crashReport = CrashReport;
                    if (crashReport != null)
                    {
                        crashReport(asyncState.Index, "Packet Spamming/DDOS");
                    }
                    Disconnect(asyncState.Index);
                    asyncState.Dispose();
                }
                else
                {
                    if (PacketAcceptLimit == 0 || PacketAcceptLimit > asyncState.PacketCount)
                    {
                        if (asyncState.RingBuffer == null)
                        {
                            asyncState.RingBuffer = new byte[length1];
                            Buffer.BlockCopy((Array)asyncState.Buffer, 0, (Array)asyncState.RingBuffer, 0, length1);
                        }
                        else
                        {
                            int length2 = asyncState.RingBuffer.Length;
                            byte[] numArray = new byte[length2 + length1];
                            Buffer.BlockCopy((Array)asyncState.RingBuffer, 0, (Array)numArray, 0, length2);
                            Buffer.BlockCopy((Array)asyncState.Buffer, 0, (Array)numArray, length2, length1);
                            asyncState.RingBuffer = numArray;
                        }
                        if (BufferLimit > 0 && asyncState.RingBuffer.Length > BufferLimit)
                        {
                            Disconnect(asyncState.Index);
                            asyncState.Dispose();
                            return;
                        }
                    }
                    if (!socket.ContainsKey(asyncState.Index))
                    {
                        asyncState.Dispose();
                    }
                    else if (socket[asyncState.Index] == null || !socket[asyncState.Index].Connected)
                    {
                        Disconnect(asyncState.Index);
                        asyncState.Dispose();
                    }
                    else
                    {
                        PacketHandler(ref asyncState);
                        asyncState.Buffer = new byte[8192];
                        if (!socket.ContainsKey(asyncState.Index))
                        {
                            asyncState.Dispose();
                        }
                        else
                        {
                            try
                            {
                                socket[asyncState.Index].BeginReceive(asyncState.Buffer, 0, socket[asyncState.Index].ReceiveBufferSize, SocketFlags.None, new AsyncCallback(DoReceive), (object)asyncState);
                            }
                            catch
                            {

                            }
                        }
                    }
                }
            }
        }

        private void PacketHandler(ref Core.ReceiveState so)
        {
            int length = so.RingBuffer.Length;
            int num = 0;
            bool flag = false;
            int count;
            while (true)
            {
                count = length - num;
                if (count >= 4)
                {
                    int int32_1 = BitConverter.ToInt32(so.RingBuffer, num);
                    if (int32_1 >= 4)
                    {
                        if (int32_1 <= count)
                        {
                            int startIndex = num + 4;
                            int int32_2 = BitConverter.ToInt32(so.RingBuffer, startIndex);
                            byte[] data;
                            if (int32_2 >= 0 && int32_2 < packetCount)
                            {
                                if (PacketId[int32_2] != null)
                                {
                                    if (int32_1 - 4 > 0)
                                    {
                                        data = new byte[int32_1 - 4];
                                        Buffer.BlockCopy((Array)so.RingBuffer, startIndex + 4, (Array)data, 0, int32_1 - 4);
                                        Core.PacketInfoArgs packetReceived = PacketReceived;
                                        if (packetReceived != null)
                                        {
                                            packetReceived(int32_1 - 4, int32_2, ref data);
                                        }
                                        PacketId[int32_2](so.Index, ref data);
                                        num = startIndex + int32_1;
                                        --so.PacketCount;
                                        flag = true;
                                    }
                                    else
                                    {
                                        data = new byte[0];
                                        Core.PacketInfoArgs packetReceived = PacketReceived;
                                        if (packetReceived != null)
                                        {
                                            packetReceived(0, int32_2, ref data);
                                        }
                                        PacketId[int32_2](so.Index, ref data);
                                        num = startIndex + int32_1;
                                        --so.PacketCount;
                                        flag = true;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                goto label_20;
                            }
                        }
                        else
                        {
                            goto label_30;
                        }
                    }
                    else
                    {
                        goto label_25;
                    }
                }
                else
                {
                    goto label_30;
                }
            }
            if (!socket.ContainsKey(so.Index))
            {
                so.Dispose();
                return;
            }
            Core.CrashReportArgs crashReport1 = CrashReport;
            if (crashReport1 != null)
            {
                crashReport1(so.Index, "NullReferenceException");
            }
            Disconnect(so.Index);
            so.Dispose();
            return;
        label_20:
            if (!socket.ContainsKey(so.Index))
            {
                so.Dispose();
                return;
            }
            Core.CrashReportArgs crashReport2 = CrashReport;
            if (crashReport2 != null)
            {
                crashReport2(so.Index, "IndexOutOfRangeException");
            }
            Disconnect(so.Index);
            so.Dispose();
            return;
        label_25:
            if (!socket.ContainsKey(so.Index))
            {
                so.Dispose();
                return;
            }
            Core.CrashReportArgs crashReport3 = CrashReport;
            if (crashReport3 != null)
            {
                crashReport3(so.Index, "BrokenPacketException");
            }
            Disconnect(so.Index);
            so.Dispose();
            return;
        label_30:
            if (count == 0)
            {
                so.RingBuffer = (byte[])null;
                so.PacketCount = 0;
            }
            else
            {
                byte[] numArray = new byte[count];
                Buffer.BlockCopy((Array)so.RingBuffer, num, (Array)numArray, 0, count);
                so.RingBuffer = numArray;
                if (!flag)
                {
                    return;
                }
                so.PacketCount = 1;
            }
        }

        public void SendDataTo(int index, byte[] data)
        {
            if (!socket.ContainsKey(index))
            {
                return;
            }
            if (socket[index] == null || !socket[index].Connected)
            {
                Disconnect(index);
            }
            else
            {
                socket[index].BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(DoSend), (object)index);
            }
        }

        public void SendDataTo(int index, byte[] data, int head)
        {
            if (!socket.ContainsKey(index))
            {
                return;
            }
            if (socket[index] == null || !socket[index].Connected)
            {
                Disconnect(index);
            }
            else
            {
                try
                {
                    byte[] buffer = new byte[head + 4];
                    Buffer.BlockCopy((Array)BitConverter.GetBytes(head), 0, (Array)buffer, 0, 4);
                    Buffer.BlockCopy((Array)data, 0, (Array)buffer, 4, head);
                    socket[index].BeginSend(buffer, 0, head + 4, SocketFlags.None, new AsyncCallback(DoSend), (object)index);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        public void SendDataToAll(byte[] data, int head)
        {
            byte[] data1 = new byte[head + 4];
            Buffer.BlockCopy((Array)BitConverter.GetBytes(head), 0, (Array)data1, 0, 4);
            Buffer.BlockCopy((Array)data, 0, (Array)data1, 4, head);
            for (int index = 0; index <= HighIndex; ++index)
            {
                if (socket.ContainsKey(index))
                {
                    SendDataTo(index, data1);
                }
            }
        }

        public void SendDataToAllBut(int index, byte[] data, int head)
        {
            byte[] data1 = new byte[head + 4];
            Buffer.BlockCopy((Array)BitConverter.GetBytes(head), 0, (Array)data1, 0, 4);
            Buffer.BlockCopy((Array)data, 0, (Array)data1, 4, head);
            for (int index1 = 0; index1 <= HighIndex; ++index1)
            {
                if (socket.ContainsKey(index1) && index1 != index)
                {
                    SendDataTo(index1, data1);
                }
            }
        }

        private void DoSend(IAsyncResult ar)
        {
            int asyncState = (int)ar.AsyncState;
            try
            {
                socket[asyncState].EndSend(ar);
            }
            catch
            {
                Core.CrashReportArgs crashReport = CrashReport;
                if (crashReport != null)
                {
                    crashReport(asyncState, "ConnectionForciblyClosedException");
                }
                Disconnect(asyncState);
            }
        }

        public bool IsConnected(int index)
        {
            if (!socket.ContainsKey(index))
            {
                return false;
            }
            if (socket[index].Connected)
            {
                return true;
            }
            Disconnect(index);
            return false;
        }

        public string GetIPv4()
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
        }

        public string ClientIp(int index)
        {
            return IsConnected(index) ? ((IPEndPoint)socket[index].RemoteEndPoint).ToString() : "[NULL]";
        }

        public void Disconnect(int index)
        {
            if (!socket.ContainsKey(index))
            {
                return;
            }
            if (socket[index] == null)
            {
                socket.Remove(index);
                unsignedIndex.Add(index);
            }
            else
            {
                socket[index].BeginDisconnect(false, new AsyncCallback(DoDisconnect), (object)index);
            }
        }

        private void DoDisconnect(IAsyncResult ar)
        {
            int asyncState = (int)ar.AsyncState;
            try
            {
                socket[asyncState].EndDisconnect(ar);
            }
            catch
            {
            }
            if (!socket.ContainsKey(asyncState))
            {
                return;
            }
            socket[asyncState].Dispose();
            socket[asyncState] = (Socket)null;
            socket.Remove(asyncState);
            unsignedIndex.Add(asyncState);
            Core.ConnectionArgs connectionLost = ConnectionLost;
            if (connectionLost == null)
            {
                return;
            }
            connectionLost(asyncState);
        }

        private int FindEmptySlot(int startIndex)
        {
            for (int index = unsignedIndex.Count - 1; index >= 0 && HighIndex == unsignedIndex[index]; --index)
            {
                --HighIndex;
            }
            if (unsignedIndex.Count > 0)
            {
                using (List<int>.Enumerator enumerator = unsignedIndex.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        int current = enumerator.Current;
                        if (HighIndex < current)
                        {
                            HighIndex = current;
                        }
                        unsignedIndex.Remove(current);
                        return current;
                    }
                }
                if (HighIndex < startIndex)
                {
                    HighIndex = startIndex;
                }
                return startIndex;
            }
            if (HighIndex < startIndex)
            {
                int key = startIndex;
                while (socket.ContainsKey(key))
                {
                    ++key;
                }
                HighIndex = key;
                return key;
            }
            while (socket.ContainsKey(HighIndex))
            {
                ++HighIndex;
            }
            return HighIndex;
        }

        public void Dispose()
        {
            StopListening();
            foreach (int key in socket.Keys)
            {
                Disconnect(key);
            }
            socket.Clear();
            socket = (Dictionary<int, Socket>)null;
            PacketId = (Core.DataArgs[])null;
            unsignedIndex.Clear();
            unsignedIndex = (List<int>)null;
            ConnectionReceived = (Core.ConnectionArgs)null;
            ConnectionLost = (Core.ConnectionArgs)null;
            CrashReport = (Core.CrashReportArgs)null;
            PacketReceived = (Core.PacketInfoArgs)null;
            TrafficReceived = (Core.TrafficInfoArgs)null;
            PacketId = (Core.DataArgs[])null;
        }

        public delegate void ConnectionArgs(int index);

        public delegate void DataArgs(int index, ref byte[] data);

        public delegate void CrashReportArgs(int index, string reason);

        public delegate void PacketInfoArgs(int size, int header, ref byte[] data);

        public delegate void TrafficInfoArgs(int size, ref byte[] data);

        public delegate void NullArgs();

        private struct ReceiveState : IDisposable
        {
            internal int Index;
            internal int PacketCount;
            internal byte[] Buffer;
            internal byte[] RingBuffer;
            internal ReceiveState(int index)
            {
                Index = index;
                PacketCount = 0;
                Buffer = new byte[8192];
                RingBuffer = (byte[])null;
            }
            public void Dispose()
            {
                Buffer = (byte[])null;
                RingBuffer = (byte[])null;
            }
        }

    }
}