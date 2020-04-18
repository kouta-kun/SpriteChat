using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Drawing;
using Timer = System.Threading.Timer;
using SpriteChat.Common;

namespace SpriteChat.Server
{
    public class SpriteChatService
    {
        Socket listener;
        readonly ManualResetEvent done = new ManualResetEvent(false);

        readonly internal System.Collections.Concurrent.ConcurrentBag<ClientObject> clientCollection = new System.Collections.Concurrent.ConcurrentBag<ClientObject>();
        readonly Thread listenThread;
        private readonly short[] mapTiles = new short[65 * 65];
        private readonly List<(ClientObject sender, string message)> lastMessages = new List<(ClientObject sender, string message)>();

        public void Stop()
        {
            listenThread.Abort();
            tt.Dispose();
            tt = null;
        }

        public SpriteChatService(string backgroundPath)
        {
            Bitmap map;
            map = (Bitmap)Image.FromFile(backgroundPath);
            for (int y = 0; y < 65; y++)
            {
                for (int x = 0; x < 65; x++)
                {
                    Color colorAtXY = map.GetPixel(y, x);
                    mapTiles[y * 65 + x] = (short)((colorAtXY.R * 40) + colorAtXY.G);
                    Console.WriteLine(y + "," + x + ": " + mapTiles[y * 65 + x] + " (from " + colorAtXY.R + " *40 + " + colorAtXY.G + ")");
                }
            }
            listenThread = new Thread(ListenerThreadLoop);
            listenThread.Start();
            tt = new Timer(TimerCallback, null, 200, 50);
        }

        private void ListenerThreadLoop()
        {
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 11020));
            listener.Listen(100);
            while (true)
            {
                done.Reset();
                //Console.WriteLine("Waiting for connection...");
                listener.BeginAccept(AcceptCallback,
                    listener);
                done.WaitOne();
            }
        }

        private void TimerCallback(object k)
        {
            //Console.WriteLine("Updating users...");
            lock (clientCollection)
            {
                var parallelLoopResult = Parallel.ForEach(clientCollection, co =>
                {
                    if (co.name != null)
                    {
                        SendStatus(co);
                        foreach (ClientObject target in clientCollection)
                        {
                            if (co != target && target.name != null)
                            {
                                StringBuilder sb_sight = new StringBuilder("P");
                                sb_sight.Append(target.name);
                                sb_sight.Append('@');
                                if (ClientObject.Distance(co, target) < 5.0)
                                {
                                    sb_sight.Append(target.position.x);
                                    sb_sight.Append(',');
                                    sb_sight.Append(target.position.y);
                                }
                                else
                                {
                                    sb_sight.Append("-1");
                                    sb_sight.Append(',');
                                    sb_sight.Append("-1");
                                }
                                sb_sight.Append(".");
                                var buf_sight = Encoding.ASCII.GetBytes(sb_sight.ToString());
                                try
                                {
                                    co.sock.Send(buf_sight, SocketFlags.None);
                                }
                                catch (Exception) { }
                            }
                        }
                        foreach (var (sender, message) in lastMessages)
                        {
                            co.sock.Send(Encoding.ASCII.GetBytes("M" + sender.name + ":" + message + "."));
                        }
                        co.rotated = false;
                        co.moved = false;
                    }
                });
                while (!parallelLoopResult.IsCompleted)
                {
                    //Console.WriteLine("Waiting for all threads to sync...");
                    Thread.Sleep(50);
                }
                lastMessages.Clear();
            }
        }

        private static void SendStatus(ClientObject state)
        {
            StringBuilder sb = new StringBuilder("self?");
            sb.Append(state.position.x).Append(",").Append(state.position.y);
            sb.Append(".");
            var buf = Encoding.ASCII.GetBytes(sb.ToString());
            try
            {
                state.sock.Send(buf, SocketFlags.None);
            }
            catch (Exception) { }
        }

        Timer tt = null;

        private void AcceptCallback(IAsyncResult ar)
        {
            //Console.WriteLine("Accepted socket");
            done.Set();
            Socket l2 = (Socket)ar.AsyncState;
            Socket handler = l2.EndAccept(ar);

            ClientObject state = new ClientObject(null);
            lock (clientCollection)
            {
                clientCollection.Add(state);
            }
            state.sock = handler;
            var tileMap = new byte[(65 * 65 * 2)];
            for (int y = 0; y < 65; y++)
                for (int x = 0; x < 65; x++)
                {
                    var t = BitConverter.GetBytes(mapTiles[y * 65 + x]);
                    tileMap[(y * 65 + x) * 2] = t[0];
                    tileMap[(y * 65 + x) * 2 + 1] = t[1];
                }
            var tileMapStr = "T" + System.Convert.ToBase64String(tileMap) + ".";
            state.sock.Send(Encoding.ASCII.GetBytes(tileMapStr));
            handler.BeginReceive(state.buffer, 0, ClientObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            //Console.WriteLine("Reading from socket");
            ClientObject state = ar.AsyncState as ClientObject;
            Socket handler = state.sock;
            int read;
            try
            {
                read = handler.EndReceive(ar);
            } catch (Exception e)
            {
                TerminateClient(state);
                return;
            }
            //Console.WriteLine("Read " + read + " bytes");
            if (read > 0)
            {
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, read));
                for (int i = 0; i < state.sb.Length; i++)
                {
                    if (state.sb[i] == '\n' || state.sb[i] == '\r')
                    {
                        state.sb.Remove(i, 1);
                        i--;
                    }
                }
                //Console.WriteLine("Current buffer: " + state.sb.ToString());
                if (state.sb.Length > 0)
                {
                    String command = state.sb.ToString();
                    if (command.Contains('.'))
                    {
                        int commandEnd = command.IndexOf('.');
                        state.sb.Clear();
                        state.sb.Append(command.Substring(commandEnd + 1));
                        command = command.Substring(0, commandEnd + 1);
                        //Console.WriteLine("Read command: " + command);
                        lock (clientCollection)
                        {
                            ExecuteCommand(state, command);
                        }
                    }
                }
                //Console.WriteLine("Ask for more");
                if (state.sock != null && state.sock.Connected)
                    handler.BeginReceive(state.buffer, 0, ClientObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
        }

        private void ExecuteCommand(ClientObject state, string command)
        {
            if (command.StartsWith("NS"))
            {
                state.name = command[2..^1];
                SendStatus(state);
            }
            else if (command.Trim() == "End.")
            {
                state.sock.Close();
                TerminateClient(state);
            }
            else if (state.name != null)
                switch (command[0])
                {
                    case 'M':
                        HandleMovement(state, command);
                        break;
                    case 'S':
                        HandleMessage(state, command);
                        break;
                }
        }

        private static void TerminateClient(ClientObject state)
        {
            state.sock = null;
            state.position.x = -1;
            state.position.y = -1;
        }

        private void HandleMessage(ClientObject state, string command) => lastMessages.Add((state, command[1..^1]));

        private void HandleMovement(ClientObject state, string command)
        {
            if (!state.moved)
            {
                var direction = ParseDirection(command[1]);
                var ox = state.position.x;
                var oy = state.position.y;
                switch (direction)
                {
                    case Direction.North:
                        if (state.position.y > 0)
                            state.position.y -= 1;
                        break;
                    case Direction.South:
                        if (state.position.y < 64)
                            state.position.y += 1;
                        break;
                    case Direction.West:
                        if (state.position.x > 0)
                            state.position.x -= 1;
                        break;
                    case Direction.East:
                        if (state.position.x < 64)
                            state.position.x += 1;
                        break;
                }
                state.moved = ox != state.position.x || oy != state.position.y;
                SendStatus(state);
            }
        }

        private Direction ParseDirection(char v)
        {
            return v switch
            {
                'N' => Direction.North,
                'E' => Direction.East,
                'S' => Direction.South,
                'W' => Direction.West,
                _ => throw new Exception(),
            };
        }
    }

    internal class ClientObject
    {
        public Socket sock;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
        public Common.Point position = new Common.Point(0,0);
        public string name;
        public bool moved = false;
        public bool rotated = false;

        public override string ToString()
        {
            return name + "@" + position.x + "," + position.y;
        }

        public static float Distance(ClientObject co, ClientObject target)
        {
            return (float)Math.Sqrt(Math.Pow(co.position.x - target.position.x, 2) + Math.Pow(co.position.y - target.position.y, 2));
        }

        public ClientObject(string name)
        {
            this.name = name;
        }
    }
}
