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
        public Filesystem.SuperBlock Super = new Filesystem.SuperBlock(52428800);
        public List<Filesystem.Inode> ilist = new List<Filesystem.Inode>();
        public List<bool> bitmap = new List<bool>();
        public List<Filesystem.Root> roots = new List<Filesystem.Root>();
        ushort inode;
        FileStream file;
        byte[] MassivByte;
        public string[] comand = new string[3];
        byte startperm = 2 | 4 | 8;
        public ushort curruser;
        public byte[,] clusters;
        private string currdir;


        public FLog LogForm;

        public MainWindow(FLog fl, string UserLogin)
        {
            for (int i = 0; i < Super.clustCount; i++)
            {
                bitmap.Add(false);
                ilist.Add(new Filesystem.Inode((ushort)i));
            }
            clusters = new byte[Super.clustCount, Super.clustSz];
            currdir = "\\";
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
            int i = 0;
            TBIn.Text = TBIn.Text + " ";
            if (!TBIn.Text.StartsWith("crtfl"))
            {
                TBOut.Text += TBIn.Text + "\r\n";
                do
                {
                    comand[i] = TBIn.Text.Substring(0, TBIn.Text.IndexOf(' '));
                    TBIn.Text = TBIn.Text.Remove(0, TBIn.Text.IndexOf(' ') + 1);
                    i++;
                } while (TBIn.Text.Length != 0);
            }
            else
            {
                do
                {
                    comand[i] = TBIn.Text.Substring(0, TBIn.Text.IndexOf(' '));
                    TBIn.Text = TBIn.Text.Remove(0, TBIn.Text.IndexOf(' ') + 1);
                    i++;
                } while (i < 2);
                comand[2] = TBIn.Text.Substring(0, TBIn.Text.Length - 1);
                TBOut.Text += comand[0] + " " + comand[1] + "\r\n";
                TBIn.Text = null;
            }
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

            //форматируем к/к
            Filesystem.SerializableRoot obj_root = new Filesystem.SerializableRoot();
            obj_root.Roots = roots;
            Filesystem.RootSerializer root_ser = new Filesystem.RootSerializer();
            root_ser.SerializeRoot("root.txt", obj_root);
        }

        private int AddFile(string Name, string Text, byte flgs)
        {
            foreach (Filesystem.Root root in roots)
            {
                if (root.name == Name)
                {
                    return -3;//Файл с таким именем существует
                }
            }
            MassivByte = Encoding.Default.GetBytes(Text);
            int clustneed = 0;
            int inodenum = 0;
            if (MassivByte.Length % Super.clustSz == 0)
                clustneed = MassivByte.Length / Super.clustSz;
            else
                clustneed = MassivByte.Length / Super.clustSz + 1;

            if (Super.freeClustCount >= clustneed && clustneed <= 10)
            {
                foreach (Filesystem.Inode inode in ilist)
                {
                    if (inode.isfree)
                    {
                        inodenum = inode.id_inode;
                        break;
                    }
                }
                for (int i = 0; i < clustneed; i++)//связываем кластеры с массивом инода и отмечаем как занятые в bitmap
                {
                    for (int j = 0; j < bitmap.Count; j++)
                    {
                        if (!bitmap[j])
                        {
                            ilist[inodenum].clst[i] = j;
                            bitmap[j] = true;
                            break;
                        }
                    }
                }
                Super.freeClustCount -= (ushort)clustneed;//Уменьшаем счетчик свободных кластеров
                ilist[inodenum].crdate = DateTime.Now;
                ilist[inodenum].chdate = DateTime.Now;
                ilist[inodenum].isfree = false;
                ilist[inodenum].uid = curruser;
                ilist[inodenum].perm = startperm;
                ilist[inodenum].flags = flgs;

                roots.Add(new Filesystem.Root(Name, inodenum));

                int sym = 0, clustnum = 0;
                while (sym < MassivByte.Length) //запись информации в кластер
                {
                    clusters[ilist[inodenum].clst[clustnum], sym % Super.clustSz] = MassivByte[sym];
                    sym++;
                    if (sym % Super.clustSz == 0)
                        clustnum++;
                }

                return inodenum; // Возвращаем номер инода
            }
            else
            {
                if (Super.freeClustCount >= clustneed)  //Не достаточно памяти
                    return -1;
                else   //Файл слишком большой
                    return -2;
            }
        }

        private int Rename(string old_name, string new_name)
        {
            int with_old = -1;
            foreach (Filesystem.Root root in roots)
            {
                if (root.name == new_name)
                    return -1; //Файл с именем new_name уже существует
                if (root.name == old_name)
                    with_old = roots.IndexOf(root);
            }
            if (with_old != -1)
            {
                roots[with_old].name = new_name;
                return 0;
            }
            else //Не найден файл с именем old_name
                return 1;
        }

        private int DelFile(string FileName)
        {
            int targroot = -1;
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i].name == FileName)
                {
                    targroot = i;
                    break;
                }
            }
            if (targroot == -1)//Файл с именем FileName не найден
                return -1;

            int[] MassivCluster = ilist[roots[targroot].idinode].clst;
            int lastclust = 0;
            for (int i = 0; i < MassivCluster.Length; i++)
            {
                if (MassivCluster[i] == -1)
                {
                    lastclust = i - 1;
                    break;
                }
            }
            for (int i = 0; i <= lastclust; i++)
            {
                for (int j = 0; j < Super.clustSz; j++)
                {
                    clusters[MassivCluster[i], j] = 0; //Очистка занятых файлом кластеров
                }
                bitmap[MassivCluster[i]] = false;
            }
            ushort currid = ilist[roots[targroot].idinode].id_inode;
            ilist[roots[targroot].idinode] = new Filesystem.Inode(currid);
            Super.freeClustCount++;

            return 0;
        }

        private int OpenFile(string FileName)
        {
            int targroot = -1;
            foreach (Filesystem.Root root in roots)
            {
                if (root.name == FileName)
                {
                    targroot = root.idinode;
                    break;
                }
            }
            if (targroot == -1)//Файл не найден
                return -1;

            if (curruser == ilist[roots[targroot].idinode].uid)
            {
                if ((ilist[roots[targroot].idinode].uid & 8) == 0) //У создателя нет прав на чтение
                    return 1;
            }
            else
            {
                if ((ilist[roots[targroot].idinode].uid & 2) == 0) //У другого пользователя нет прав на чтение
                    return 1;
            }

            int[] MassivCluster = ilist[roots[targroot].idinode].clst;
            int lastclust = 0;
            for (int i = 0; i < MassivCluster.Length; i++)
            {
                if (MassivCluster[i] == -1)
                {
                    lastclust = i - 1;
                    break;
                }
            }
            string outtext = null; //Строка для вывода
            for (int i = 0; i <= lastclust; i++)
            {
                for (int j = 0; (j < Super.clustSz && clusters[MassivCluster[i], j] != 0); j++)
                {
                    outtext += (char)clusters[MassivCluster[i], j];
                }
            }
            TBOut.Text += outtext + "\r\n";
            return 0;
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
                        "crtfl file text - создать файл с именем file и содержимым text\r\n"+
                        "crtdir name - создать папку с именем name\r\n" +
                        "rm file - удалить файл file\r\n" +
                        "cat file - показать содержимое файла file\r\n" +
                        "pwd - узнать адрес текущей директории\r\n" +
                        "ls - вывести список файлов в текущей директрии\r\n";
                    return true;
                case "addusr":
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
                    if (comand[1] != null && comand[2] != null)
                    {
                        int err = Rename(comand[1], comand[2]);
                        if (err == -1) TBOut.Text += "Файл с именем " + comand[2] + " уже существует\r\n";
                        else if (err == 0) TBOut.Text += "Файл успешно переименован\r\n";
                        else if (err == 1) TBOut.Text += "Файл с именем " + comand[1] + " не найден\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "crtfl":
                    if (comand[1] != null && comand[2] != null)
                    {
                        int err = AddFile(comand[1], comand[2], 0);
                        if (err == -1) TBOut.Text += "Не достаточно памяти для записи файла\r\n";
                        else if (err == -2) TBOut.Text += "Файл слишком большой\r\n";
                        else if (err == -3) TBOut.Text += "Файл с таким именем уже существует\r\n";
                        else TBOut.Text += "Файл успешно создан\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "crtdir":
                    if (comand[1] != null)
                        AddFile(comand[1],"", 2);
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "rm":
                    if (comand[1] != null)
                    {
                        int err = DelFile(comand[1]);
                        if (err == -1) TBOut.Text += "Файл с таким именем не найден\r\n";
                        else if (err == 0) TBOut.Text += "Файл успешно удалён\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "cat":
                    if (comand[1] != null)
                    {
                        int err = OpenFile(comand[1]);
                        if (err == -1) TBOut.Text += "Файл не найден\r\n";
                        else if (err == 1) TBOut.Text += "У вас недостаточно прав для чтения этого файла\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "pwd":
                    TBOut.Text += currdir + "\r\n";
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
