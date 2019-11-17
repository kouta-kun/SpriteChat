using System;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using SpriteChat.Common;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SpriteChat
{
    public partial class SpriteChatClient : UserControl
    {
        private const int TileSize = 16;
        Socket socket;
        Direction? move = null;
        Common.Point myPosition = null;
        private int[,] mapTiles = new int[65, 65];
        string name;

        List<(string sender, string message, DateTime receivedAt)> messageList = new List<(string sender, string message, DateTime receivedAt)>();

        ConcurrentDictionary<string, Common.Point> seen;

        public SpriteChatClient(IPAddress ipAddress, string Name)
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.name = Name;
            seen = new ConcurrentDictionary<string, Common.Point>();
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 11020);
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            string nameCommand = "NS" + Name + ".";
            var nameBuffer = Encoding.ASCII.GetBytes(nameCommand);
            socket.Send(nameBuffer);
            socket.BeginReceive(buffer, 0, 1024, 0, DataLoop, null);
            t.Interval = 1000 / 20;
            t.Tick += RedrawHandler;
            t.Start();
            this.Focus();
        }

        private void RedrawHandler(object sender, EventArgs e)
        {
            this.Refresh();
        }

        System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
        byte[] buffer = new byte[1024];

        internal void SendMessage(string text)
        {
            var str = "S" + text + ".";
            socket.Send(Encoding.ASCII.GetBytes(str));
            this.Focus();
        }

        private StringBuilder responseBuffer = new StringBuilder();
        private ManualResetEvent RedrawEvent = new ManualResetEvent(false);

        private void DataLoop(IAsyncResult ar)
        {
            int bytesRec = socket.EndReceive(ar);
            //Console.WriteLine("bytes: " + bytesRec);
            if (bytesRec > 0)
            {
                responseBuffer.Append(Encoding.ASCII.GetString(buffer, 0, bytesRec));
                var currentInfo = responseBuffer.ToString();
                //Console.WriteLine("Buffer size: " + responseBuffer.Length);
                while (currentInfo.Contains('.'))
                {
                    int commandEnd = currentInfo.IndexOf('.');
                    responseBuffer.Clear();
                    responseBuffer.Append(currentInfo.Substring(commandEnd + 1));
                    currentInfo = currentInfo.Substring(0, commandEnd + 1);
                    //Console.WriteLine("info: " + currentInfo);
                    //Console.WriteLine("buffer: " + responseBuffer.ToString());
                    if (currentInfo.StartsWith("self?"))
                    {
                        //Console.WriteLine("Got self info");
                        var values = currentInfo.Split('?')[1];
                        values = values.Substring(0, values.Length - 1);
                        var xy = (from x in values.Split(',') select int.Parse(x)).ToArray();
                        myPosition = new Common.Point(xy[0], xy[1]);
                        if (move != null)
                        {
                            var k = Enum.GetName(typeof(Direction), move)[0];
                            var cmd = "M" + k + ".";
                            socket.Send(Encoding.ASCII.GetBytes(cmd));
                            move = null;
                        }
                    }
                    else if (currentInfo.StartsWith("P"))
                    {
                        var info = currentInfo.Split('@');
                        var pointsInfo = info[1].Substring(0, info[1].Length - 1);
                        var name = info[0].Substring(1);
                        var points = (from x in pointsInfo.Split(',') select int.Parse(x)).ToArray();
                        seen[name] = new Common.Point(points[0], points[1]);
                    }
                    else if (currentInfo.StartsWith("T"))
                    {
                        var b64Data = currentInfo.Substring(1, currentInfo.Length - 2);
                        var data = Convert.FromBase64String(b64Data);
                        for (int y = 0; y < 65; y++)
                        {
                            for (int x = 0; x < 65; x++)
                            {
                                mapTiles[y, x] = BitConverter.ToInt16(data, (y * 65 + x) * 2);
                                Console.WriteLine(y + "," + x + ": " + mapTiles[y, x]);
                            }
                        }
                    }
                    else if (currentInfo.StartsWith("M"))
                    {
                        var sender = currentInfo.Substring(1, currentInfo.IndexOf(":") - 1);
                        var message = currentInfo.Substring(currentInfo.IndexOf(":") + 1);
                        messageList.Add((sender, message, DateTime.Now));
                        Console.WriteLine(sender + ": " + message);
                    }
                    currentInfo = responseBuffer.ToString();
                }
            }
            socket.BeginReceive(buffer, 0, 1024, 0, DataLoop, null);
            this.RedrawEvent.Set();
        }

        private void CivilTerrorClient_Paint(object self, PaintEventArgs e)
        {
            if (myPosition == null) return;
            int xOffset = (Width / TileSize) / 2;
            int yOffset = (Height / TileSize) / 2;
            var g = e.Graphics;
            var backBrush = Brushes.White;
            g.FillRectangle(backBrush, new Rectangle(0, 0, 520, 520));
            var linePen = Pens.Black;
            var playerBrush = Brushes.Green;
            var otherPlayerBrush = Brushes.Yellow;
            var farPlayerBrush = Brushes.Orange;
            // (int)myPosition.x - xOffset
            // (int)myPosition.x + xOffset
            int startX = (int)myPosition.x - xOffset;
            int endX = (int)myPosition.x + xOffset + 1;
            int startY = (int)myPosition.y - yOffset;
            int endY = (int)myPosition.y + yOffset + 1;
            for (int y = startY; y < endY; y++)
            {
                if (y < 0) y = 0;
                for (int x = startX; x < endX; x++)
                {
                    if (x < 0) x = 0;
                    g.DrawImage(TextureCache.Overworld(mapTiles[y, x]), new Rectangle((x - startX) * 16, (y - startY) * 16, TileSize, TileSize));
                }
            }
            if (myPosition != null)
            {
                g.DrawImage(TextureCache.Char(0), (float)(myPosition.x - startX) * 16, (float)(myPosition.y - startY - 1) * 16, 16, 32);
                g.DrawString(name, SystemFonts.MessageBoxFont, Brushes.Aqua, new PointF((float)(myPosition.x - startX) * 16, (float)(myPosition.y - startY - 1.5) * 16));
                foreach (var (_, message, time) in messageList.Where(p => p.sender == name && (DateTime.Now - p.receivedAt).TotalSeconds < 5))
                {
                    SizeF messageSize = g.MeasureString(message, SystemFonts.MessageBoxFont);
                    PointF messagePoint = new PointF((float)(myPosition.x - startX) * 16, (float)(myPosition.y - startY + 2.5) * 16);
                    g.FillRectangle(Brushes.White, messagePoint.X, messagePoint.Y, messageSize.Width, messageSize.Height);
                    g.DrawRectangle(Pens.Black, messagePoint.X, messagePoint.Y, messageSize.Width, messageSize.Height);
                    g.DrawString(message, SystemFonts.MessageBoxFont, Brushes.Aqua, messagePoint);
                    g.DrawLine(Pens.Black, messagePoint, new PointF((float)(myPosition.x - startX) * 16, (float)(myPosition.y - startY + 1) * 16));
                }
            }
            foreach (var k in seen.Where(p => myPosition.CanSee(p.Value)))
            {
                g.DrawImage(TextureCache.Char(0), (float)(k.Value.x - startX) * 16, (float)(k.Value.y - startY - 1) * 16, 16, 32);
                g.DrawString(k.Key, SystemFonts.MessageBoxFont, Brushes.Aqua, new PointF((float)(k.Value.x - startX) * 16, (float)(k.Value.y - startY - 1.5) * 16));
                foreach (var (_, message, time) in messageList.Where(p => p.sender == k.Key && (DateTime.Now - p.receivedAt).TotalSeconds < 5))
                {
                    SizeF messageSize = g.MeasureString(message, SystemFonts.MessageBoxFont);
                    PointF messagePoint = new PointF((float)(k.Value.x - startX) * 16, (float)(k.Value.y - startY + 2.5) * 16);
                    g.FillRectangle(Brushes.White, messagePoint.X, messagePoint.Y, messageSize.Width, messageSize.Height);
                    g.DrawRectangle(Pens.Black, messagePoint.X, messagePoint.Y, messageSize.Width, messageSize.Height);
                    g.DrawString(message, SystemFonts.MessageBoxFont, Brushes.Aqua, messagePoint);
                    g.DrawLine(Pens.Black, messagePoint, new PointF((float)(k.Value.x - startX) * 16, (float)(k.Value.y - startY + 1) * 16));
                }
            }
            //g.DrawEllipse(Pens.Red, new Rectangle((int)(myPosition.x - startX - 5) * 16, (int)(myPosition.y - startY - 5) * 16, 10 * 16, 10 * 16));
            g.Flush();
        }

        private void CivilTerrorClient_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W:
                    move = (Direction.North);
                    break;
                case Keys.S:
                    move = (Direction.South);
                    break;
                case Keys.A:
                    move = (Direction.West);
                    break;
                case Keys.D:
                    move = (Direction.East);
                    break;
            }
        }
    }
}
