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


        public FLog LogForm;

        public MainWindow(FLog fl, string UserLogin)
        {
            for (int i = 0; i < Super.clustCount; i++)
            {
                bitmap.Add(false);
                ilist.Add(new Filesystem.Inode());
            }
            clusters = new byte[Super.clustCount, Super.clustSz];
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

            //форматируем к/к
            Filesystem.SerializableRoot obj_root = new Filesystem.SerializableRoot();
            obj_root.Roots = roots;
            Filesystem.RootSerializer root_ser = new Filesystem.RootSerializer();
            root_ser.SerializeRoot("root.txt", obj_root);
        }

        private int AddFile(string Name, string Text, byte flgs)
        {
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

        /*private int CreateFile(string Context, byte permis, byte flags_state)
        {
            MassivByte = Encoding.Default.GetBytes(Context);
            file = new FileStream("Data.txt", FileMode.Open);
            int i, InodNumber = -1;
            int Last = -1;
            
            //Сколько кластеров нужно
            ushort clustneed = 0;
            if (MassivByte.Length % Super.clustSz == 0)
                clustneed = (ushort)(MassivByte.Length / Super.clustSz);
            else
                clustneed = (ushort)(MassivByte.Length / Super.clustSz + 1);
            int m = clustneed;

            //если < 10, то ищем свободный инод
            if (clustneed <= 10)
            {
                foreach (Filesystem.Inode inode in ilist)
                {
                    if (inode.isfree == true)
                    {
                        InodNumber = inode.id_inode;
                        inode.fileSz = (uint)Context.Length;
                        inode.isfree = false;
                        inode.uid = curruser;
                        inode.crdate = DateTime.Now;
                        inode.chdate = DateTime.Now;
                        inode.perm = permis;
                        inode.flags = flags_state;
                        Last = 0;
                        break;
                    }
                }

                if (Last == -1)
                {
                    file.Close();
                    MessageBox.Show("Нет свободных дескрипторов", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return -1;
                }

                Last = -1;
                int LengthMassive = MassivByte.Length;
                int position = 0;
                int bit = -1;
                int clustSize = Super.clustSz;

                for (i = 0; i < bitmap.Count; i++)
                {
                    if (bitmap[i] == false)
                    {
                        bit = i;
                        bitmap[i] = true;
                        break;
                    }
                }

                ilist[InodNumber].clst[0] = (ushort)bit;
                Last = i;

                clustneed--;

                file.Seek(i * Super.clustSz, SeekOrigin.Begin);
                if (Super.clustSz < LengthMassive)
                {
                    file.Write(MassivByte, 0, Super.clustSz);
                    LengthMassive -= Super.clustSz;
                    position += Super.clustSz;
                }
                else
                {
                    file.Write(MassivByte, 0, LengthMassive);
                }

                if (Last == -1)
                {
                    file.Close();
                    MessageBox.Show("Недостаточно места для записи файла", "Ошибка записи", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return -1;
                }

                if (clustneed > 0)
                {
                    int p = 1;
                    for (i = 0; (i < bitmap.Count && clustneed != 0); i++)
                    {
                        if (bitmap[i] == false)
                        {
                            bitmap[i] = true;
                            ilist[InodNumber].clst[p] = (ushort)i;
                            p++;
                            Last = i;
                            clustneed--;
                            file.Seek((i) * Super.clustSz, SeekOrigin.Begin);
                            if (Super.clustSz < LengthMassive)
                            {
                                file.Write(MassivByte, position, Super.clustSz);
                                LengthMassive -= Super.clustSz;
                                position += Super.clustSz;
                            }
                            else
                            {
                                file.Write(MassivByte, position, LengthMassive);
                            }
                        }
                    }
                }
                file.Close();
                return InodNumber;
            }
            else
            {
                file.Close();
                MessageBox.Show("Файл слишком большой", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }

        // Сохранить файл
        private void SaveFile(int nodeNumber, string Context)
        {
            MassivByte = Encoding.Default.GetBytes(Context);
            file = new FileStream("Data.txt", FileMode.Open);
            ilist[nodeNumber].fileSz = (ushort)Context.Length;
            ilist[nodeNumber].chdate = DateTime.Now;

            //смотрим, сколько нужно всего кластеров
            int WaitCountClasters = 0;
            if (MassivByte.Length % Super.clustSz == 0)
                WaitCountClasters = MassivByte.Length / Super.clustSz;
            else
                WaitCountClasters = MassivByte.Length / Super.clustSz + 1;

            //если < 10 - ищем свободный инод
            if (WaitCountClasters <= 10)
            {
                int er = 0;
                for (int j = 0; j < 10; j++)
                {
                    if (ilist[nodeNumber].clst[j] != -1)
                        er++;
                }
                if (WaitCountClasters == er)
                {
                    int LengthMassive = MassivByte.Length;
                    int position = 0;
                    int clasterSize = Super.clustSz;
                    for (int p = 0; p < er; p++)
                    {
                        file.Seek(ilist[nodeNumber].clst[p] * Super.clustSz, SeekOrigin.Begin);
                        if (Super.clustSz < LengthMassive)
                        {
                            file.Write(MassivByte, 0, Super.clustSz);
                            LengthMassive -= Super.clustSz;
                            position += Super.clustSz;
                        }
                        else
                        {
                            file.Write(MassivByte, position, LengthMassive);
                        }
                    }
                }

                if ((WaitCountClasters <= er) || (WaitCountClasters >= er))
                {
                    for (int j = 0; j < 10; j++)
                    {
                        if (ilist[nodeNumber].clst[j] != -1)
                            bitmap[ilist[nodeNumber].clst[j]] = false;
                        ilist[nodeNumber].clst[j] = -1;
                        er++;
                    }
                    int Last = -1, i = 0;
                    int LengthMassive = MassivByte.Length;
                    int position = 0;
                    int bit = -1;
                    int clasterSize = Super.clustSz;

                    for (i = 0; i < bitmap.Count; i++)
                    {
                        if (bitmap[i] == false)
                        {
                            bit = i;
                            bitmap[i] = true;
                            break;
                        }
                    }

                    ilist[nodeNumber].clst[0] = bit;
                    Last = i;

                    WaitCountClasters--;

                    file.Seek(i * Super.clustSz, SeekOrigin.Begin);
                    if (Super.clustSz < LengthMassive)
                    {
                        file.Write(MassivByte, 0, Super.clustSz);
                        LengthMassive -= Super.clustSz;
                        position += Super.clustSz;
                    }
                    else
                    {
                        file.Write(MassivByte, 0, LengthMassive);
                    }

                    if (WaitCountClasters > 0)
                    {
                        int p = 1;
                        for (i = 0; (i < bitmap.Count && WaitCountClasters != 0); i++)
                        {
                            if (bitmap[i] == false)
                            {
                                bitmap[i] = true;
                                ilist[nodeNumber].clst[p] = i;
                                p++;
                                Last = i;
                                WaitCountClasters--;
                                file.Seek((i) * Super.clustSz, SeekOrigin.Begin);
                                if (Super.clustSz < LengthMassive)
                                {
                                    file.Write(MassivByte, position, Super.clustSz);
                                    LengthMassive -= Super.clustSz;
                                    position += Super.clustSz;
                                }
                                else
                                {
                                    file.Write(MassivByte, position, LengthMassive);
                                }
                            }
                        }
                    }
                }
                file.Close();
            }
            else
            {
                file.Close();
                MessageBox.Show("Файл слишком большой", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void NewFile(string Name, string Text, byte permlist, byte flags)
        {

            if (Name == "" || Name.Length > 14)
            {
                MessageBox.Show("Имя файла должно быть от 1 до 14 символов.");
            }

            else
            {
                int p1 = -1;
                foreach (Filesystem.Root root in roots)
                {
                    if (root.name == Name)
                    {
                        p1 = 1;
                        break;
                    }
                }
                if (p1 == -1)
                {

                    int InodNumber = CreateFile(Text, permlist, 16);
                    if (InodNumber != -1)
                    {

                        foreach (Filesystem.Root p in roots)
                        {
                            if (p.name == "^^^^^^^^^^^^^^")
                            {
                                roots.Remove(p);
                                break;
                            }
                        }
                        {
                            Filesystem.Root TestRoot = new Filesystem.Root(Name, InodNumber,);
                            roots.Add(TestRoot);
                            MessageBox.Show("Файл создан");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Создано максимальное количество файлов");
                    }
                }
                else
                {
                    MessageBox.Show("Файл уже существует");
                }
            }
        }

        private void AddFile(string filename, string text)
        {
            NewFile(filename, text, chmod, 0);
        }

        private void AddDir(string filename, string text)
        {
            NewFile(filename, text, chmod, 16);
        }*/

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
                        AddFile(comand[1], comand[2], 0);
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
