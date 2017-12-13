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
    public struct MassivFile
    {
        public string name;
        public string text;
        public MassivFile(string Name, string Text)
        {
            name = Name;
            text = Text;
        }
    };
    public partial class MainWindow : Form
    {
        public List<Users> UsList = new List<Users>();
        public Filesystem.SuperBlock Super = new Filesystem.SuperBlock(20971520);
        public List<Filesystem.Inode> ilist = new List<Filesystem.Inode>();
        public List<bool> bitmap = new List<bool>();
        public List<Filesystem.Root> currdir = new List<Filesystem.Root>();
        byte[] MassivByte;
        public string[] comand = new string[3];
        byte startperm = 2 | 4 | 8;
        public ushort curruser;
        public byte[,] clusters;
        private string currpath = "ROOT";

        public FLog LogForm;

        public MainWindow(FLog fl, string UserLogin)
        {
            for (int i = 0; i < Super.clustCount; i++)
            {
                bitmap.Add(false);
                ilist.Add(new Filesystem.Inode((ushort)i));
            }
            clusters = new byte[Super.clustCount, Super.clustSz];
            LogForm = fl;
            BinaryFormatter bin = new BinaryFormatter();
            FileStream stream = new FileStream("Dir\\" + currpath, FileMode.Create);
            bin.Serialize(stream, currdir);
            stream.Close();
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
                do
                {
                    comand[i] = TBIn.Text.Substring(0, TBIn.Text.IndexOf(' '));
                    TBIn.Text = TBIn.Text.Remove(0, TBIn.Text.IndexOf(' ') + 1);
                    i++;
                    if (i > 2)
                        break;
                } while (TBIn.Text.Length != 0);
                TBOut.Text += comand[0] + " " + comand[1];
                if (comand[0] != "nusr")
                    TBOut.Text += " " + comand[2] + "\r\n";
                else
                    TBOut.Text += "\r\n";
            }
            else
            {
                do
                {
                    comand[i] = TBIn.Text.Substring(0, TBIn.Text.IndexOf(' '));
                    TBIn.Text = TBIn.Text.Remove(0, TBIn.Text.IndexOf(' ') + 1);
                    i++;
                } while (i < 2);
                if (TBIn.Text.Length > 0)
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

        private void AddUser(string Login, string Password, bool ChngFile)
        {
            if (Password.Length < 4)
            {
                TBOut.Text += "Пароль должен содержать минимум 4 символа\r\n";
                return;
            }
            bool exept = false;
            foreach (Users user in UsList)
            {
                if (Login == user.login)
                {
                    exept = true;
                    break;
                }
            }
            if (!exept)
            {
                UsList.Add(new Users(GetID(Password + Login), Login, Password));
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
            FileStream stream = new FileStream("superblock", FileMode.Create);
            formater.Serialize(stream, Super);
            stream.Close();

            //форматируем inode
            stream = new FileStream("inodes", FileMode.Create);
            formater.Serialize(stream, ilist);
            stream.Close();

            //форматируем bitmap
            stream = new FileStream("bitmap", FileMode.Create);
            formater.Serialize(stream, bitmap);
            stream.Close();

            //форматируем к/к
            stream = new FileStream("root", FileMode.Create);
            formater.Serialize(stream, currdir);
            stream.Close();

            //форматируем кластеры с данными
            stream = new FileStream("clust", FileMode.Create);
            formater.Serialize(stream, clusters);
            stream.Close();
        }

        private bool Loading()
        {
            BinaryFormatter formater = new BinaryFormatter();
            //Загружаем суперблок
            if ((File.Exists("superblock") && File.Exists("inodes") && File.Exists("bitmap") && File.Exists("root") && File.Exists("clust")))
            {
                FileStream stream = new FileStream("superblock", FileMode.Open);
                Super = (Filesystem.SuperBlock)formater.Deserialize(stream);
                stream.Close();

                //Загружаем inode
                stream = new FileStream("inodes", FileMode.Open);
                ilist = (List<Filesystem.Inode>)formater.Deserialize(stream);
                stream.Close();


                //Загружаем bitmap
                stream = new FileStream("bitmap", FileMode.Open);
                bitmap = (List<bool>)formater.Deserialize(stream);
                stream.Close();


                //Загружаем к/к
                stream = new FileStream("root", FileMode.Open);
                currdir = (List<Filesystem.Root>)formater.Deserialize(stream);
                stream.Close();
                stream = new FileStream("Dir/ROOT", FileMode.Create);
                formater.Serialize(stream, currdir);
                stream.Close();

                //Загружаем кластеры данных
                stream = new FileStream("clust", FileMode.Open);
                clusters = (byte[,])formater.Deserialize(stream);
                stream.Close();

                currpath = "ROOT";
                return true;
            }
            else
                return false;
        }

        private string GetCurrPath()
        {
            return currpath;
        }

        private int AddFile(string FileName, string Text)
        {
            foreach (Filesystem.Root root in currdir)
            {
                if (root.name == FileName)
                    return -3;//Файл с таким именем существует
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

                currdir.Add(new Filesystem.Root(FileName, inodenum));

                int sym = 0, clustnum = 0;
                while (sym < MassivByte.Length) //Запись информации в кластер
                {
                    clusters[ilist[inodenum].clst[clustnum], sym % Super.clustSz] = MassivByte[sym];
                    sym++;
                    if (sym % Super.clustSz == 0)
                        clustnum++;
                }
                BinaryFormatter binform = new BinaryFormatter();
                FileStream stream = new FileStream("Dir\\" + currpath, FileMode.Create);
                binform.Serialize(stream, currdir);
                stream.Close();
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

        private int AddDir(string DirName)
        {
            foreach (Filesystem.Root root in currdir)
            {
                if (root.name == DirName)
                {
                    return -3;//Файл с таким именем существует
                }
            }
            BinaryFormatter rootser = new BinaryFormatter();
            List<Filesystem.Root> dirroot = new List<Filesystem.Root>();
            FileStream stream = new FileStream("Dir\\" + currpath + "#" + DirName, FileMode.Create);
            rootser.Serialize(stream, dirroot);
            stream.Close();
            MassivByte = Encoding.Default.GetBytes(File.ReadAllText("Dir\\" + currpath + "#" + DirName));
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
                ilist[inodenum].flags = 2;

                currdir.Add(new Filesystem.Root(DirName, inodenum));

                int sym = 0, clustnum = 0;
                while (sym < MassivByte.Length) //Запись информации в кластер
                {
                    clusters[ilist[inodenum].clst[clustnum], sym % Super.clustSz] = MassivByte[sym];
                    sym++;
                    if (sym % Super.clustSz == 0)
                        clustnum++;
                }
                BinaryFormatter binform = new BinaryFormatter();
                stream = new FileStream("Dir\\" + currpath, FileMode.Create);
                binform.Serialize(stream, currdir);
                stream.Close();
                OpenDir(DirName);
                dirroot.Add(new Filesystem.Root("..", -1));
                OpenDir("..");
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

        private int OpenDir(string DirName)
        {
            if (DirName.StartsWith("ROOT"))//Если путь начинается с ROOT - путь абсолютный
            {
                while (GetCurrPath() != "ROOT")
                    OpenDir("..");
                DirName = DirName.Substring(DirName.IndexOf("#")+1);
            }
            string nowgoto;
            if (DirName.IndexOf('#') > -1)
                nowgoto = DirName.Substring(0, DirName.IndexOf('#'));
            else
                nowgoto = DirName;
            int targroot = -1;
            int targinode = -1;
            if (nowgoto == "..")
            {
                if (currpath != "ROOT")
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    currpath = currpath.Substring(0, currpath.LastIndexOf('#'));
                    FileStream Newstream = new FileStream("Dir\\" + currpath, FileMode.Open);
                    currdir = (List<Filesystem.Root>)bin.Deserialize(Newstream);
                    Newstream.Close();
                    if (DirName.IndexOf('#') > -1 && DirName.Length > 1)
                    {
                        DirName = DirName.Substring(DirName.IndexOf('#') + 1);
                        OpenDir(DirName);
                    }
                    return 0;
                }
                else
                    return -2;
            }
            for (int i = 0; i < currdir.Count; i++)
            {
                if (currdir[i].name == nowgoto)
                {
                    targinode = currdir[i].idinode;
                    targroot = i;
                    break;
                }
            }
            if ((targinode == -1) || ((ilist[targinode].flags & 2) == 0))
                return -1;//Папка на найдена
            if ((ilist[targinode].perm & 8) == 0 && curruser== ilist[targinode].uid)
                return -3; //Нет прав на чтение
            if ((ilist[targinode].perm & 2) == 0 && curruser != ilist[targinode].uid)
                return -3; //Нет прав на чтение
            currpath += "#" + nowgoto;
            int lastclust = 9;
            for (int i = 0; i < ilist[targinode].clst.Length; i++)
            {
                if (ilist[targinode].clst[i] == -1)
                {
                    lastclust = i - 1;
                    break;
                }
            }
            int[] MassivCluster = ilist[currdir[targroot].idinode].clst;
            byte[] MassivByte = new byte[(lastclust + 1) * Super.clustSz];
            for (int i = 0; i <= lastclust; i++)
            {
                for (int j = 0; (j < Super.clustSz); j++)
                    MassivByte[i * Super.clustSz + j] = clusters[MassivCluster[i], j];
            }
            FileStream stream = new FileStream("Dir\\" + currpath, FileMode.Open);
            BinaryFormatter binform = new BinaryFormatter();
            currdir = (List<Filesystem.Root>)binform.Deserialize(stream);
            stream.Close();
            if (DirName.IndexOf('#') > -1 && DirName.Length > 1)
            {
                DirName = DirName.Substring(DirName.IndexOf('#')+1);
                OpenDir(DirName);
            }
            return 0;
        }

        private int Append(string FileName, string text)
        {
            if (text.Length == 0)
                return 0;
            int targinode = -1;
            int targroot = -1;
            for (int i = 0; i < currdir.Count; i++)
            {
                if (currdir[i].name == FileName)
                {
                    targinode = currdir[i].idinode;
                    targroot = i;
                    break;
                }
            }
            if (targroot == -1)
                return -5;//Файл не найден
            if (((ilist[targinode].perm & 4) == 0 && curruser == ilist[targinode].uid) || (curruser != ilist[targinode].uid && (ilist[targinode].perm & 1) == 0))
                return -3; //Нет прав
            if ((ilist[targinode].flags & 2) == 2)
                return -4;//Это папка
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
            if (lastlclust == -1)
            {
                int clustneed = 0;
                int inodenum = targinode;
                if (MassivByte.Length % Super.clustSz == 0)
                    clustneed = MassivByte.Length / Super.clustSz;
                else
                    clustneed = MassivByte.Length / Super.clustSz + 1;

                if (Super.freeClustCount >= clustneed && clustneed <= ilist[0].clst.Length)
                {
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
                    int sym = 0, clustnum = 0;
                    while (sym < MassivByte.Length) //Запись информации в кластер
                    {
                        clusters[ilist[inodenum].clst[clustnum], sym % Super.clustSz] = MassivByte[sym];
                        sym++;
                        if (sym % Super.clustSz == 0)
                            clustnum++;
                    }
                    BinaryFormatter binform = new BinaryFormatter();
                    FileStream stream = new FileStream("Dir\\" + currpath, FileMode.Create);
                    binform.Serialize(stream, currdir);
                    stream.Close();
                    return 0; //Успешно
                }
                else
                {
                    if (Super.freeClustCount >= clustneed)  //Недостаточно памяти
                        return -1;
                    else   //Файл слишком большой
                        return -2;
                }
            }
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
                ilist[targinode].chdate = DateTime.Now;
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
            foreach (Filesystem.Root root in currdir)
            {
                if (root.name == new_name)
                    return -1; //Файл с именем new_name уже существует
                if (root.name == old_name)
                    with_old = currdir.IndexOf(root);
            }
            if (with_old != -1)
            {
                if (((ilist[currdir[with_old].idinode].perm & 4) != 0 && curruser == ilist[currdir[with_old].idinode].uid) ^ (curruser != ilist[currdir[with_old].idinode].uid && (ilist[currdir[with_old].idinode].perm & 1) != 0))
                {
                    currdir[with_old].name = new_name;
                    ilist[currdir[with_old].idinode].chdate = DateTime.Now;
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
            for (int i = 0; i < currdir.Count; i++)
            {
                if (currdir[i].name == FileName)
                {
                    rootdel = currdir[i];
                    targroot = i;
                    break;
                }
            }
            if (targroot == -1)//Файл с именем FileName не найден
                return -1;

            if (ilist[rootdel.idinode].uid == curruser)
            {
                if ((ilist[rootdel.idinode].perm & 4) == 0)
                    return 1;//У создателя нет прав на изменение файла
            }
            else
            {
                if ((ilist[rootdel.idinode].perm & 1) == 0)
                    return 1;//У другого пользователя нет прав на изменение файла
            }

            if ((ilist[rootdel.idinode].flags & 2) == 2)//Удаляемый файл является директорией
                return DelDir(FileName);

            int[] MassivCluster = ilist[rootdel.idinode].clst;
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
                    clusters[MassivCluster[i], j] = 0; //Очистка занятых файлом кластеров
                bitmap[MassivCluster[i]] = false;
            }
            ushort currid = ilist[rootdel.idinode].id_inode;
            ilist[rootdel.idinode] = new Filesystem.Inode(currid);
            Super.freeClustCount += (uint)lastclust + 1;
            currdir.Remove(rootdel);

            BinaryFormatter binform = new BinaryFormatter();
            FileStream stream = new FileStream("Dir\\" + currpath, FileMode.Create);
            binform.Serialize(stream, currdir);
            stream.Close();

            return 0;
        }

        private int CanWrite(string FileName)
        {
            int targinode = -1;
            for (int i = 0; i < currdir.Count; i++)
            {
                if (currdir[i].name == FileName)
                {
                    targinode = currdir[i].idinode;
                    break;
                }
            }
            if (ilist[targinode].uid == curruser)
            {
                if ((ilist[targinode].perm & 4) == 4)
                    return 1;//Есть права
            }
            else
            {
                if ((ilist[targinode].perm & 1) == 1)
                    return 1;//Есть право
            }
            return -1;//Нет прав
        }

        private int CanRead(string FileName)
        {
            int targinode = -1;
            for (int i = 0; i < currdir.Count; i++)
            {
                if (currdir[i].name == FileName)
                {
                    targinode = currdir[i].idinode;
                    break;
                }
            }
            if (ilist[targinode].uid == curruser)
            {
                if ((ilist[targinode].perm & 8) == 8)
                    return 1;//Есть права
            }
            else
            {
                if ((ilist[targinode].perm & 2) == 2)
                    return 1;//Есть право
            }
            return -1;//Нет прав
        }

        private int DelDir(string DirName)
        {
            OpenDir(DirName);
            if (currdir.Count != 0)
            {
                foreach (var file in currdir)
                {
                    if (CanWrite(file.name) == -1)//Если хоть один файл внутри нельзя удалить
                    {
                        OpenDir("..");
                        return 1;//Нет прав для удаления файлов внутри
                    }
                }
                while (currdir.Count > 0)
                    DelFile(currdir[0].name);//Постоянно удаляется первый в списке поэтому список смещается и мы удаляем только первый элемент
            }
            OpenDir("..");
            Filesystem.Root delroot = new Filesystem.Root();
            for (int i = 0; i < currdir.Count; i++)
            {
                if (currdir[i].name == DirName)
                {
                    delroot = currdir[i];
                    break;
                }
            }
            int[] MassivCluster = ilist[delroot.idinode].clst;
            int id = delroot.idinode;
            ilist[id] = new Filesystem.Inode((ushort)id);
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
                    clusters[MassivCluster[i], j] = 0; //Очистка занятых файлом кластеров
                bitmap[MassivCluster[i]] = false;
            }
            Super.freeClustCount += (uint)lastclust + 1;
            currdir.Remove(delroot);
            BinaryFormatter binform = new BinaryFormatter();
            FileStream stream = new FileStream("Dir\\" + currpath, FileMode.Create);
            binform.Serialize(stream, currdir);
            stream.Close();
            return 0;
        }

        private string OpenFile(string FileName, ref string Stream)
        {
            int targroot = -1;
            int targinode = -1;
            for (int i = 0; i < currdir.Count; i++)
            {
                if (currdir[i].name == FileName)
                {
                    targinode = currdir[i].idinode;
                    targroot = i;
                    break;
                }
            }

            int[] MassivCluster = ilist[currdir[targroot].idinode].clst;
            int lastclust = 9;
            for (int i = 0; i < MassivCluster.Length; i++)
            {
                if (MassivCluster[i] == -1)
                {
                    lastclust = i - 1;
                    break;
                }
            }
            byte[] MassivByte = new byte[(lastclust + 1) * Super.clustSz];
            for (int i = 0; i <= lastclust; i++)
            {
                for (int j = 0; (j < Super.clustSz); j++)
                {
                    MassivByte[i * Super.clustSz + j] = clusters[MassivCluster[i], j];
                }
            }
            Stream = Encoding.Default.GetString(MassivByte);
            return Stream;
        }

        private int OpenFile(string FileName)
        {
            int targroot = -1;
            int targinode = -1;
            for (int i = 0; i < currdir.Count; i++)
            {
                if (currdir[i].name == FileName)
                {
                    targinode = currdir[i].idinode;
                    targroot = i;
                    break;
                }
            }
            if (targinode == -1)//Файл не найден
                return -1;

            if ((ilist[targinode].flags & 2) == 2)
                return -2;//Это каталог

            if (((ilist[currdir[targroot].idinode].perm & 8) == 0 && curruser == ilist[currdir[targroot].idinode].uid) ^ ((ilist[currdir[targroot].idinode].perm & 2) == 0 && curruser != ilist[currdir[targroot].idinode].uid)) //Нет прав на чтение
                return 1;

            int[] MassivCluster = ilist[currdir[targroot].idinode].clst;
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
                for (int j = 0; (j < Super.clustSz); j++)
                {
                    MassivByte[i * Super.clustSz + j] = clusters[MassivCluster[i], j];
                }
            }
            outtext = Encoding.Default.GetString(MassivByte);
            TBOut.Text += outtext;
            TBOut.Text += "\r\n";
            return 0;
         }

        private int CopyFile(string FileToCopy, string PathToCopy)
        {
            int targinode = -1;
            int targroot = -1;
            for (int i = 0; i < currdir.Count; i++)
            {
                if (currdir[i].name == FileToCopy)
                {
                    targinode = currdir[i].idinode;
                    targroot = i;
                    break;
                }
            }
            if (targroot == -1)
                return -1;//Файл не найден
            if (curruser == ilist[targinode].uid)
            {
                if ((ilist[targinode].perm & 8) == 0)
                    return -2;//Нет прав
            }
            else
            {
                if ((ilist[targinode].perm & 2) == 0)
                    return -2;//Нет прав
            }
            string CopyText = null;
            string Start = GetCurrPath();//Начинаем путь отсюда
            if ((ilist[targinode].flags & 2) == 2)//Копируем папку
            {
                OpenDir(FileToCopy);
                List<MassivFile> Massivfiletext = new List<MassivFile>();
                for (int i = 0; i < currdir.Count; i++)
                {
                    if (CanRead(currdir[i].name) == 1)
                    {
                        if ((ilist[currdir[i].idinode].flags & 2) == 0)//Если внутри копируемой директории файл
                        {
                            OpenFile(currdir[i].name, ref CopyText);
                            Massivfiletext.Add(new MassivFile(currdir[i].name, CopyText));
                        }
                        else//Если внутри копируемой директории еще директория
                            return -3;
                    }
                    else
                        return -2;
                }
                OpenDir("..");
                OpenDir(PathToCopy);
                AddDir(FileToCopy);
                OpenDir(FileToCopy);
                for (int i = 0; i < Massivfiletext.Count; i++)
                    AddFile(Massivfiletext[i].name, Massivfiletext[i].text);
                OpenDir("..");
            }
            else
            {
                OpenFile(FileToCopy, ref CopyText);
                OpenDir(PathToCopy);
                AddFile(FileToCopy, CopyText);
            }
            OpenDir(Start);
            return 0;
        }

        private int ChangePerm(string FileName, string new_perm)
        {
            int permtoint = 0;
            int targroot = -1;
            int targinode = -1;
            for (int i = 0; i < currdir.Count; i++)
            {
                if (currdir[i].name == FileName)
                {
                    targinode = currdir[i].idinode;
                    targroot = i;
                    break;
                }
            }
            if (targroot == -1)
                return -1; //Файл не найден

            if (curruser == ilist[targinode].uid)//Изменить права может только создатель файла
            {
                if (int.TryParse(new_perm, out permtoint))
                {
                    ilist[targinode].perm = (byte)permtoint;
                    ilist[targinode].chdate = DateTime.Now;
                    return 0;
                }
                else
                    return -3; //Неверно задан параметр прав
            }
            else
                return -2;//Попытка изменить права другим пользователем
        }

        private void DisplayFileList()
        {
            TBOut.Text += "Имя\tПрава\tДата создания/Изменения\t\tРазмер\tДир-я\tID создателя\r\n";
            foreach (Filesystem.Root root in currdir)
            {
                TBOut.Text += root.name + "\t";
                if ((ilist[root.idinode].perm & 8) == 8)
                    TBOut.Text += "r";
                else
                    TBOut.Text += "_";
                if ((ilist[root.idinode].perm & 4) == 4)
                    TBOut.Text += "w";
                else
                    TBOut.Text += "_";
                if ((ilist[root.idinode].perm & 2) == 2)
                    TBOut.Text += "r";
                else
                    TBOut.Text += "_";
                if ((ilist[root.idinode].perm & 1) == 1)
                    TBOut.Text += "w";
                else
                    TBOut.Text += "_";
                TBOut.Text += "\t";
                TBOut.Text += ilist[root.idinode].crdate.ToString()+"/"+ ilist[root.idinode].chdate.ToString()+"\t";
                int lastclust = 10;
                for (int i = 0; i < ilist[root.idinode].clst.Length; i++)
                {
                    if (ilist[root.idinode].clst[i] == -1)
                    {
                        lastclust = i;
                        break;
                    }
                }
                TBOut.Text += lastclust * Super.clustSz + "\t";
                if ((ilist[root.idinode].flags & 2) == 2)
                    TBOut.Text += "Да\t";
                else
                    TBOut.Text += "Нет\t";
                TBOut.Text += ilist[root.idinode].uid + "\r\n";

            }
        }

        private bool GetComand(string cmd)
        {
            switch (cmd)
            {
                case "help":
                    TBOut.Text += "nusr <name> <pass> - создание пользователя с именем name и паролем pass\r\n"+
                        "rmusr <name> <pass> - удалить пользователя с именем name и паролем pass\r\n"+
                        "lsusr - вывести список существующих пользователей\r\n"+
                        "cp <file> <path> - копировать файл file в директорию path\r\n"+
                        "rnm <old_name> <new_name> - переименовать файл old_name в new_name\r\n"+
                        "crtfl <file>( <text>) - создать файл с именем file( и содержимым text)\r\n"+
                        "crtdir <name> - создать директорию с именем name\r\n" +
                        "append <file> <text> - добавить text в конец файла file\r\n" +
                        "cd <path> - перейти по директориям path\r\n" +
                        "rm <file> - удалить файл/директорию file\r\n" +
                        "cat <file> - показать содержимое файла file\r\n" +
                        "chmod <file> <perm> - изменить права доступа к файлу file на perm\r\n" +
                        "pwd - узнать адрес текущей директории\r\n" +
                        "ls - вывести список файлов в текущей директрии\r\n" +
                        "push - сохранить текущее состояние ФС в физ. память\r\n" +
                        "pull - загрузить состояние ФС, сохранённое в физ. памяти\r\n";
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
                case "append":
                    if (comand[1] != null && comand[2] != null)
                    {
                        int err = Append(comand[1], comand[2]);
                        if (err == -1) TBOut.Text += "Недостаточно памяти для записи файла\r\n";
                        else if (err == -2) TBOut.Text += "Файл слишком большой\r\n";
                        else if (err == -3) TBOut.Text += "Не достаточно прав для записи в файл\r\n";
                        else if (err == -4) TBOut.Text += "Действие неприменимо к каталогу\r\n";
                        else if (err == -5) TBOut.Text += "Файл не найден\r\n";
                        else TBOut.Text += "Файл успешно изменён\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "cp":
                    if (comand[1] != null && comand[2] != null)
                    {
                        int err = CopyFile(comand[1], comand[2]);
                        if (err == -1) TBOut.Text += "Файл не найден\r\n";
                        else if (err == -2) TBOut.Text += "Не достаточно прав на копирование этого файла/директории\r\n";
                        else if (err == -3) TBOut.Text += "Копируемая директория содержит директории\r\n";
                        else TBOut.Text += "Файл/директория скопирован\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
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
                    if (comand[1] != null)
                    {
                        int err;
                        if (comand[2] != null)
                            err = AddFile(comand[1], comand[2]);
                        else
                            err = err = AddFile(comand[1], "");
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
                    {
                        int err = AddDir(comand[1]);
                        if (err == -1) TBOut.Text += "Недостаточно памяти для создания директории\r\n";
                        else if (err == -2) TBOut.Text += "Директория слишком большая\r\n";
                        else if (err == -3) TBOut.Text += "Файл с таким именем уже существует\r\n";
                        else TBOut.Text += "Директория успешно создана\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "cd":
                    if (comand[1] != null)
                    {
                        int err = OpenDir(comand[1]);
                        if (err == -1) TBOut.Text += "Директория с таким именем не найдена\r\n";
                        else if (err == -2) TBOut.Text += "ROOT не имеет родительской директории\r\n";
                        else if (err == -3) TBOut.Text += "У вас нет прав для просмотра содержимого директории\r\n";
                        else TBOut.Text += currpath + "\r\n";
                    }
                    else
                        TBOut.Text += "Введены не все параметры\r\n";
                    break;
                case "rm":
                    if (comand[1] != null)
                    {
                        int err = DelFile(comand[1]);
                        if (err == -1) TBOut.Text += "Файл/директория с таким именем не найден\r\n";
                        else if (err == 0) TBOut.Text += "Файл/директория успешно удален\r\n";
                        else if (err == 1) TBOut.Text += "У вас недостаточно прав для удаления файла/директории\r\n";
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
                        else if (err == -2) TBOut.Text += "Действие неприменимо к каталогу. Используйте команду cd\r\n";
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
                    TBOut.Text += GetCurrPath() + "\r\n";
                    break;
                case "ls":
                    DisplayFileList();
                    break;
                case "push":
                    string Path = GetCurrPath();
                    OpenDir("ROOT");
                    Formating();
                    OpenDir(Path);
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