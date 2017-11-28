using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace KursOS
{
    public partial class MainWindow : Form
    {
        ushort usCounter = 0;
        public Filesystem.SuperBlock SB = new Filesystem.SuperBlock();
        public List<Users> UsList = new List<Users>();

        public Users AddUser(ushort UsCount, string Name, string Password)
        {
            return new Users(UsCount, Name, Password);
        }

        public FLog LogForm;
        public MainWindow(FLog fl)
        {
            LogForm = fl;
            InitializeComponent();

            string fileForUsers = File.ReadAllText("../../UsrList.sys");
            int j = 0;
            do
            {
                string login = null;
                string pass = null;
                do
                {
                    login += fileForUsers[j].ToString();
                    j++;
                } while (fileForUsers[j].ToString() != "]");
                login = login.Substring(1);
                j += 3;
                do
                {
                    pass += fileForUsers[j].ToString();
                    j++;
                } while (fileForUsers[j] != '\r');
                UsList.Add(AddUser((ushort)UsList.Count, login, pass));
                j += 2;
            } while (j != fileForUsers.Length);
        }

        private void BEnter_Click(object sender, EventArgs e)
        {

            TBOut.Text += TBIn.Text + "\r\n";
            TBIn.Clear();
            TBIn.Focus();
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            LogForm.Visible = true;
        }
    }
}
