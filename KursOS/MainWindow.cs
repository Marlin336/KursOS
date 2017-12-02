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
        byte[] byteArray;
        public string[] comand = new string[3];
        byte chmod = 4 | 8;
        public ushort curruser;
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
                AddUser(login, pass, false);
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

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            LogForm.Visible = true;
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
            if (!GetComand(comand[0]))
                TBOut.Text += "Неверная команда\r\n";
            TBOut.SelectionStart = TBOut.TextLength;
            TBOut.ScrollToCaret();
        }

        private void AddUser(string Name, string Password, bool ChngFile)
        {
            bool exept = false;
            foreach (Users user in UsList)
            {
                if (Name == user.login)
                {
                    exept = true;
                    break;
                }
            }
            if (!exept)
            {
                UsList.Add(new Users(GetID(Password + Name), Name, Password));
                if (ChngFile)
                    ResetUserFile();
            }
            else
                TBOut.Text += "Пользователь с таким именем уже существует\r\n";
        }

        private ushort GetID(string str)
        {
            return (ushort)str.GetHashCode();
        }

        private int DelUser(string UserName, string Pass)
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

        private int CreateFile(string Context)
        {
            byteArray = Encoding.Default.GetBytes(Context);
            file = new FileStream("Data.txt", FileMode.Open);

            //Сколько кластеров нужно
            ushort clustneed = 0;
            if (byteArray.Length % Super.clustSz == 0)
                clustneed = (ushort)(byteArray.Length / Super.clustSz);
            else
                clustneed = (ushort)(byteArray.Length / Super.clustSz + 1);
        }

        private bool GetComand(string cmd)
        {
            switch (cmd)
            {
                case "help":
                    TBOut.Text += "nusr name pass - создание пользователя с именем name и паролем pass\r\n"+
                        "rmusr name pass - удалить пользователя с именем name и паролем pass\r\n"+
                        "lsusr - вывести список существующих пользователей\r\n"+
                        "cp stpath finpath - копировать файл stpath в место finpath\r\n"+
                        "rnm path new_name - переименовать файл path в new_name\r\n"+
                        "crt file - создать файл с именем file\r\n"+
                        "rm file - удалить файл file\r\n"+
                        "pwd - узнать адрес текущей директории\r\n"+
                        "ls - вывести список файлов в текущей директрии\r\n";
                    return true;
                case "nusr":
                    if (comand[1] != null && comand[2] != null)
                        AddUser(comand[1], comand[2], true);
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "rmusr":
                    if (comand[1] != null && comand[2] != null)
                    {
                        int err = DelUser(comand[1], comand[2]);
                        if (err == -1) TBOut.Text += "Неверный логин\r\n";
                        else if (err == 0) TBOut.Text += "Неверный пароль\r\n";
                        else if (err == 1) TBOut.Text += "Пользователь удален\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "lsusr":
                    TBOut.Text += DisplayUserList();
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
