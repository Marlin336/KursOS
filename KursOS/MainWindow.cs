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
        public Filesystem.SuperBlock Super = new Filesystem.SuperBlock(20971520);
        public List<Filesystem.Inode> ilist = new List<Filesystem.Inode>();
        public List<bool> bitmap = new List<bool>();
        public List<Filesystem.Root> roots = new List<Filesystem.Root>();
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
            if (!TBIn.Text.StartsWith("crtfl") && !TBIn.Text.StartsWith("append"))
            {
                TBOut.Text += TBIn.Text + "\r\n";
                do
                {
                    comand[i] = TBIn.Text.Substring(0, TBIn.Text.IndexOf(' '));
                    TBIn.Text = TBIn.Text.Remove(0, TBIn.Text.IndexOf(' ') + 1);
                    i++;
                    if (i > 2)
                        break;
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
            }
            TBIn.Clear();
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
            BinaryFormatter formater = new BinaryFormatter();
            //форматируем суперблок
            FileStream stream = new FileStream("superblock.sys", FileMode.Create);
            formater.Serialize(stream, Super);
            stream.Close();

            //форматируем inode
            stream = new FileStream("inodes.sys", FileMode.Create);
            formater.Serialize(stream, ilist);
            stream.Close();

            //форматируем bitmap
            stream = new FileStream("bitmap.sys", FileMode.Create);
            formater.Serialize(stream, bitmap);
            stream.Close();

            //форматируем к/к
            stream = new FileStream("root.sys", FileMode.Create);
            formater.Serialize(stream, roots);
            stream.Close();

            //форматируем кластеры с данными
            stream = new FileStream("clust.sys", FileMode.Create);
            formater.Serialize(stream, clusters);
            stream.Close();
        }

        private bool Loading()
        {
            BinaryFormatter formater = new BinaryFormatter();
            //Загружаем суперблок
            if ((File.Exists("superblock.sys") && File.Exists("inodes.sys") && File.Exists("bitmap.sys") && File.Exists("root.sys") && File.Exists("clust.sys")))
            {
                FileStream stream = new FileStream("superblock.sys", FileMode.Open);
                Super = (Filesystem.SuperBlock)formater.Deserialize(stream);
                stream.Close();

                //Загружаем inode
                stream = new FileStream("inodes.sys", FileMode.Open);
                ilist = (List<Filesystem.Inode>)formater.Deserialize(stream);
                stream.Close();


                //Загружаем bitmap

                stream = new FileStream("bitmap.sys", FileMode.Open);
                bitmap = (List<bool>)formater.Deserialize(stream);
                stream.Close();


                //Загружаем к/к

                stream = new FileStream("root.sys", FileMode.Open);
                roots = (List<Filesystem.Root>)formater.Deserialize(stream);
                stream.Close();


                //Загружаем кластеры данных

                stream = new FileStream("clust.sys", FileMode.Open);
                clusters = (byte[,])formater.Deserialize(stream);
                stream.Close();
                return true;
            }
            else
                return false;
        }

        private int AddFile(string Name, string Text, byte flg)
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

            if (Super.freeClustCount >= clustneed && clustneed <= ilist[0].clst.Length)
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
                ilist[inodenum].flags = flg;

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
                if (Super.freeClustCount >= clustneed)  //Недостаточно памяти
                    return -1;
                else   //Файл слишком большой
                    return -2;
            }
        }

        private int Append(string FileName, string text)//Не проверено!!!
        {
            int targinode = -1;
            foreach (Filesystem.Root root in roots)
            {
                if (root.name == FileName)
                {
                    targinode = root.idinode;
                    break;
                }
            }
            if (targinode == -1)
                return -1;//Файл не найден
            if (((ilist[roots[targinode].idinode].perm & 4) == 0 && curruser == ilist[roots[targinode].idinode].uid) || (curruser != ilist[roots[targinode].idinode].uid && (ilist[roots[targinode].idinode].perm & 1) == 0))
                return -3; //Нет прав
                int lastlclust = 9;
            for (int i = 0; i < ilist[targinode].clst.Length; i++)
            {
                if (ilist[targinode].clst[i] == -1)
                {
                    lastlclust = i - 1;
                    break;
                }
            }
            MassivByte = Encoding.Default.GetBytes(text);
            int firstfreebyte = Super.clustSz;
            for (int i = 0; i < Super.clustSz; i++)
            {
                if (clusters[ilist[targinode].clst[lastlclust], i] == 0)
                {
                    firstfreebyte = i;
                    break;
                }
            }
            if (Super.clustSz - firstfreebyte >= MassivByte.Length)//Размер добавочной строки меньше или равно оставшемуся в кластере месту
            {
                for (int i = 0; i < MassivByte.Length; i++)
                    clusters[ilist[targinode].clst[lastlclust], firstfreebyte + i] = MassivByte[i];
                return 0;
            }
            else//Если добавочная строка не поместиться в оставшееся в кластере место
            {
                int stopindex = 0;//Индекс байта в MassivByte, на котором остановились когда дозаполнили кластер
                for (int i = 0; firstfreebyte + i < Super.clustSz; i++)
                {
                    clusters[ilist[targinode].clst[lastlclust], firstfreebyte + i] = MassivByte[i];
                    stopindex = i;
                }//Дописали в кластер информации столько, сколько вместилось
                int residual = MassivByte.Length - stopindex - 1;
                int clustcount;
                if (residual % Super.clustSz == 0)
                    clustcount = residual / Super.clustSz;
                else
                    clustcount = residual / Super.clustSz + 1;
                if (Super.freeClustCount >= clustcount && clustcount < ilist[targinode].clst.Length - lastlclust)
                {
                    for (int i = lastlclust + 1; i < clustcount + lastlclust + 1; i++)//связываем кластеры с массивом инода и отмечаем как занятые в bitmap
                    {
                        for (int j = 0; j < bitmap.Count; j++)
                        {
                            if (!bitmap[j])
                            {
                                ilist[targinode].clst[i] = j;
                                bitmap[j] = true;
                                break;
                            }
                        }
                    }
                    Super.freeClustCount -= (ushort)clustcount;//Уменьшаем счетчик свободных кластеров
                    ilist[targinode].chdate = DateTime.Now;
                    //Дописываем оставшуюся информацию
                    int sym = stopindex + 1;
                    int clustnum = lastlclust+1;
                    int numerator = 0;
                    while (sym < MassivByte.Length) //запись информации в кластер
                    {
                        clusters[ilist[targinode].clst[clustnum], numerator % Super.clustSz] = MassivByte[sym];
                        numerator++;
                        sym++;
                        if (numerator%Super.clustSz == 0 && numerator != 0)
                            clustnum++;
                    }

                    return 0; //Дозапись прошла успешно
                }
                else
                    return -2; //Файл слишком большой

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
                if (((ilist[roots[with_old].idinode].perm & 4) != 0 && curruser == ilist[roots[with_old].idinode].uid) ^ (curruser != ilist[roots[with_old].idinode].uid && (ilist[roots[with_old].idinode].perm & 1) != 0))
                {
                    roots[with_old].name = new_name;
                    ilist[roots[with_old].idinode].chdate = DateTime.Now;
                    return 0;
                }
                else
                    return -2;
            }
            else //Не найден файл с именем old_name
                return 1;
        }

        private int DelFile(string FileName)
        {
            Filesystem.Root rootdel = new Filesystem.Root();
            int targroot = -1;
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i].name == FileName)
                {
                    rootdel = roots[i];
                    targroot = i;
                    break;
                }
            }
            if (targroot == -1)//Файл с именем FileName не найден
                return -1;

            if (ilist[roots[targroot].idinode].uid == curruser)
            {
                if ((ilist[roots[targroot].idinode].perm & 4) == 0)
                    return 1;//У создателя нет прав на изменение файла
            }
            else
            {
                if ((ilist[roots[targroot].idinode].perm & 1) == 0)
                    return 1;//У другого пользователя нет прав на изменение файла
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
            roots.Remove(rootdel);

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
                if ((ilist[roots[targroot].idinode].perm & 8) == 0) //У создателя нет прав на чтение
                    return 1;
            }
            else
            {
                if ((ilist[roots[targroot].idinode].perm & 2) == 0) //У другого пользователя нет прав на чтение
                    return 1;
            }

            int[] MassivCluster = ilist[roots[targroot].idinode].clst;
            int lastclust = 9;
            for (int i = 0; i < MassivCluster.Length; i++)
            {
                if (MassivCluster[i] == -1)
                {
                    lastclust = i - 1;
                    break;
                }
            }
            string outtext = null; //Строка для вывода
            byte[] MassivByte = new byte[(lastclust +1) * Super.clustSz];
            for (int i = 0; i <= lastclust; i++)
            {
                for (int j = 0; (j < Super.clustSz && clusters[MassivCluster[i], j] != 0); j++)
                {
                    MassivByte[i * Super.clustSz + j] = clusters[MassivCluster[i], j];
                }
            }
            outtext = Encoding.Default.GetString(MassivByte);
            TBOut.Text += outtext;
            TBOut.Text += "\r\n";
            return 0;
         }

        private int ChangePerm(string FileName, string new_perm)
        {
            int permtoint = 0;
            int targroot = -1;
            foreach (Filesystem.Root root in roots)
            {
                if (root.name == FileName)
                {
                    targroot = root.idinode;
                    break;
                }
            }
            if (targroot == -1)
                return -1; //Файл не найден

            if (curruser == ilist[roots[targroot].idinode].uid)//Изменить права может только создатель файла
            {
                if (int.TryParse(new_perm, out permtoint))
                {
                    ilist[roots[targroot].idinode].perm = (byte)permtoint;
                    return 0;
                }
                else
                    return -3; //Неверно задан параметр прав
            }
            else
                return -2;//Попытка изменить права другим пользователем
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
                        "chmod file perm - изменить права доступа к файлу file на perm\r\n" +
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
                case "append":
                    if (comand[1] != null && comand[2] != null)
                    {
                        int err = Append(comand[1], comand[2]);
                        if (err == -1) TBOut.Text += "Недостаточно памяти для записи файла\r\n";
                        else if (err == -2) TBOut.Text += "Файл слишком большой\r\n";
                        else if (err == -3) TBOut.Text += "Не достаточно прав для записи в файл\r\n";
                        else TBOut.Text += "Файл успешно изменён\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
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
                        else TBOut.Text += "У вас недостаточно прав\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "crtfl":
                    if (comand[1] != null && comand[2] != null)
                    {
                        int err = AddFile(comand[1], comand[2], 0);
                        if (err == -1) TBOut.Text += "Недостаточно памяти для записи файла\r\n";
                        else if (err == -2) TBOut.Text += "Файл слишком большой\r\n";
                        else if (err == -3) TBOut.Text += "Файл с таким именем уже существует\r\n";
                        else TBOut.Text += "Файл успешно создан\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "crtdir":
                    if (comand[1] != null)
                        AddFile(comand[1], "", 2);
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "rm":
                    if (comand[1] != null)
                    {
                        int err = DelFile(comand[1]);
                        if (err == -1) TBOut.Text += "Файл с таким именем не найден\r\n";
                        else if (err == 0) TBOut.Text += "Файл успешно удалён\r\n";
                        else if (err == 1) TBOut.Text += "У вас недостаточно прав для удаления файла\r\n";
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
                case "chmod":
                    if (comand[1] != null && comand[2] != null)
                    {
                        int err = ChangePerm(comand[1], comand[2]);
                        if (err == -1) TBOut.Text += "Файл не найден\r\n";
                        else if (err == -2) TBOut.Text += "Права доступа может менять только создатель файла\r\n";
                        else if (err == -3) TBOut.Text += "Неверно задан параметр прав\r\n";
                        else TBOut.Text += "Права доступа изменены\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "pwd":
                    TBOut.Text += currdir + "\r\n";
                    break;
                case "ls":
                    break;
                case "push":
                    Formating();
                    break;
                case "pull":
                    Loading();
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
//Организовать папки