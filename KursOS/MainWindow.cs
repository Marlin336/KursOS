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
        public List<Users> UsList = new List<Users>();
        public Filesystem.SuperBlock Super = new Filesystem.SuperBlock();
        public List<Filesystem.Inode> ilist = new List<Filesystem.Inode>();
        public List<byte> bitmap = new List<byte>();
        public List<Filesystem.Root> roots = new List<Filesystem.Root>();
        int inode;
        FileStream file;
        public string[] comand;
        bool[] chmod = { true, true, false, false };
        public ushort curruser;

        public Users AddUser(string Name, string Password)
        {
            return new Users(GetID(Password+Name+Password), Name, Password);
        }

        private ushort GetID(string str)
        {
            ushort id = 0;
            str = str.Substring(str.Length / 4);
            foreach (char c in str)
                id += c;
            return id;
        }

        public FLog LogForm;
        public MainWindow(FLog fl, string UserLogin)
        {
            LogForm = fl;
            InitializeComponent();
            //Запись в список пользователей из файла
            #region
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
                j += 2;
                do
                {
                    pass += fileForUsers[j].ToString();
                    j++;
                } while (fileForUsers[j] != '\r');
                UsList.Add(AddUser(login, pass));
                j++;
            } while (j != fileForUsers.Length);
            foreach (Users user in UsList)
            {
                if (user.login == UserLogin)
                {
                    curruser = user.uid;
                    break;
                }
            }
            #endregion
        }

        private void BEnter_Click(object sender, EventArgs e)
        {
            /*TBOut.Text += TBIn.Text + "\r\n";
            TBIn.Clear();
            TBIn.Focus();*/
            TBOut.Text += DisplayUserList();
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            LogForm.Visible = true;
        }

        public int DelUser(string UserName, string Pass)
        {
            foreach (Users user in UsList)
            {
                if (user.login == UserName)
                {
                    if (user.password == Pass)
                    {   //Если совпали логин и пароль
                        UsList.Remove(user);
                        ResetUserFile();
                        return 1;
                    }
                    else
                        //Если пароль не совпал
                        return 0;
                }
            }
            //Если логин не совпал
            return -1;
        }

        private void ResetUserFile()
        {
            File.Delete("../../UsrList.sys");
            string context = null;
            foreach (Users user in UsList)
                context += "[" + user.login + "]\r" + user.password + "\r";
            File.WriteAllText("../../UsrList.sys", context);
        }

        public string DisplayUserList()
        {
            string context = "UID\tLogin\r\n\r\n";
            foreach (Users user in UsList)
            {
                if (user.uid == curruser)
                    context += user.uid + "\t[" + user.login + "]\r\n";
                else
                    context += user.uid + "\t" + user.login + "\r\n";
            }
            return context;
        }
    }
}
