using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            SpriteChat.Server.SpriteChatService scs = new SpriteChat.Server.SpriteChatService();
        }
    }
}
