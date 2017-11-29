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
using System.Windows;
using System.Runtime.Serialization.Formatters.Binary;

namespace KursOS
{
    public partial class MainWindow : Form
    {
        public List<Users> UsList = new List<Users>();
        public Filesystem.SuperBlock Super = new Filesystem.SuperBlock();
        public List<Filesystem.Inode> ilist = new List<Filesystem.Inode>();
        public List<byte> bitmap = new List<byte>();
        public List<Filesystem.Root> roots = new List<Filesystem.Root>();
        ushort inode;
        FileStream file;
        public string[] comand = new string[3];
        byte chmod = 4 | 8;
        public ushort curruser;

        public void AddUser(string Name, string Password)
        {
            UsList.Add(new Users(GetID(Password + Name + Password), Name, Password));
            ResetUserFile();
        }

        public ushort GetID(string str)
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
            //Запись в List пользователей из файла
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
                AddUser(login, pass);
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
            TBOut.Text += TBIn.Text + "\r\n";
            int i = 0;
            TBIn.Text = TBIn.Text + " ";
            do
            {
                comand[i] = TBIn.Text.Substring(0, TBIn.Text.IndexOf(' '));
                TBIn.Text = TBIn.Text.Remove(0, TBIn.Text.IndexOf(' ') + 1);
                i++;
            } while (TBIn.Text.Length != 0);
            TBIn.Focus();
            GetComand(comand[0]);
            //TBOut.Text += DisplayUserList();
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            LogForm.Visible = true;
        }

        public int DelUser(string UserName, string Pass)
        {
            bool exflag = false;
            foreach (Users user in UsList)
            {
                if (user.login == UserName)
                {
                    if (user.password == Pass)
                    {   //Если совпали логин и пароль
                        if (curruser == user.uid)
                            exflag = true;
                        UsList.Remove(user);
                        ResetUserFile();
                        if (exflag)
                            Close();
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

        private void Formating()
        {
            //форматируем суперблок
            BinaryFormatter superblform = new BinaryFormatter();
            FileStream file = new FileStream("SuperBlock.txt", FileMode.Create);
            superblform.Serialize(file, Super);
            file.Close();

            //форматируем inode
            Filesystem.SerializableInode obj_inode = new Filesystem.SerializableInode();
            obj_inode.Inodes = ilist;
            Filesystem.InodeSerializer inode_ser = new Filesystem.InodeSerializer();
            inode_ser.SerializeInode("inodes.txt", obj_inode);

            //форматируем bitmap
            Filesystem.SerializableBitmap obj_bitmap = new Filesystem.SerializableBitmap();
            obj_bitmap.Bitmap = bitmap;
            Filesystem.BitmapSerializer bitmap_ser = new Filesystem.BitmapSerializer();
            bitmap_ser.SerializeBitmap("bitmap.txt", obj_bitmap);

            //заполнение к/к
            Filesystem.SerializableRoot obj_root = new Filesystem.SerializableRoot();
            obj_root.Roots = roots;
            Filesystem.RootSerializer root_ser = new Filesystem.RootSerializer();
            root_ser.SerializeRoot("root.txt", obj_root);
        }

        public bool GetComand(string cmd)
        {
            switch (cmd)
            {
                case "help":
                    TBOut.Text += "nusr name pass - соззание пользователя с именем name и паролем pass\r"+
                        "rmusr name pass - удалить пользователя с именем name и паролем pass\r"+
                        "lsusr - вывести список пользователей"+
                        "cp stpath finpath - копировать файл stpath в место finpath\r"+
                        "rnm path new_name - переименовать файл path в new_name\r"+
                        "crt file - создать файл с именем file"+
                        "rm file - удалить файл file"+
                        "pwd - узнать адрес текущей директории"+
                        "ls - вывести список файлов в текущей директрии";
                    return true;
                case "nusr":
                    AddUser(comand[1], comand[2]);
                    break;
                case "rmusr":
                    DelUser(comand[1], comand[2]);
                    break;
                case "lsusr":
                    break;
                case "cp":
                    break;
                case "rnm":
                    break;
                case "crt":
                    break;
                case "rm":
                    break;
                case "pwd":
                    break;
                case "ls":
                    break;
                default:
                    return false;
            }
            for (int i = 0; i < comand.Length; i++)
                comand[i] = null;
            return true;
        }
    }
}
