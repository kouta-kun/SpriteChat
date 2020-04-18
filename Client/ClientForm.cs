using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpriteChat
{
    public partial class ClientForm : Form
    {
        private SpriteChatClient cli;

        public ClientForm()
        {
            InitializeComponent();
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            IPAddress ip = Dns.GetHostEntry(ipBox.Text).AddressList.FirstOrDefault();
            if (ip != null)
            {
                this.cli = new SpriteChatClient(ip, nameBox.Text)
                {
                    Location = new Point(15, 40)
                };
                this.Controls.Add(cli);
                this.Refresh();
            }
        }
        private void messageBtn_Click(object sender, EventArgs e)
        {
            cli.SendMessage(ChatBox.Text);
        }
    }
}
