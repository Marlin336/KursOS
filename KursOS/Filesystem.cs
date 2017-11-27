using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace KursOS
{

    public class Filesystem
    {
        List<Users> UsList = new List<Users>();
        List<Groups> GrList = new List<Groups>();
        Inode[] ilist;
        SuperBlock SB = new SuperBlock();
        /*Суперблок*/
        [Serializable]
        public class SuperBlock : ISerializable
        {
            public char[] FSName = { 'N', 'P', 'F', 'S' };
            public ushort clustSz = 4096;
            public uint clustCount;
            public ushort ilistSz;
            public ushort freeinodeSz;
            public ushort freeblckcount;

            public SuperBlock() { }

            public SuperBlock(SerializationInfo sInfo, StreamingContext contextArg)
            {
                FSName = (char[])sInfo.GetValue("FSName", typeof(char[]));
                clustSz = (ushort)sInfo.GetValue("clustSz", typeof(ushort));
                clustCount = (uint)sInfo.GetValue("clustCount", typeof(uint));
                ilistSz = (ushort)sInfo.GetValue("ilistSz", typeof(ushort));
                freeinodeSz = (ushort)sInfo.GetValue("freeinodeSz", typeof(ushort));
                freeblckcount = (ushort)sInfo.GetValue("freeblckcount", typeof(ushort));
            }

            public void GetObjectData(SerializationInfo sInfo, StreamingContext contextArg)
            {
                sInfo.AddValue("FSName", FSName);
                sInfo.AddValue("clustSz", clustSz);
                sInfo.AddValue("clustCount", clustCount);
                sInfo.AddValue("ilistSz", ilistSz);
                sInfo.AddValue("freeinodeSz", freeinodeSz);
                sInfo.AddValue("freeblckcount", freeblckcount);
            }
        }

        /*Битовая карта*/
        public class BitMap
        {
            public byte[] map;
        }

        /*Структура inode*/
        [Serializable]
        public class Inode : ISerializable
        {
            public ushort id_inode;
            public ushort perm;
            public byte flags;
            /*0x1  readonly
              0x2  invisible
              0x4  system
              0x8  vol mark
              0x10 directory
              0x20 archive*/
            public uint fileSz;
            public ushort uid;
            public ushort gid;
            public DateTime chdate;
            public DateTime crdate;

            public Inode(ushort ID_node, ushort Permissions, byte Flags, uint FileSz,
                ushort UID, ushort GID, DateTime ChangeDate, DateTime CreateDate)
            {
                id_inode = ID_node;
                perm = Permissions;
                flags = Flags;
                fileSz = FileSz;
                uid = UID;
                gid = GID;
                chdate = ChangeDate;
                crdate = CreateDate;
            }

            public Inode(SerializationInfo sInfo, StreamingContext contextArg)
            {
                id_inode = (ushort)sInfo.GetValue("id_inode", typeof(ushort));
                perm = (ushort)sInfo.GetValue("perm", typeof(ushort));
                flags = (byte)sInfo.GetValue("flags", typeof(byte));
                fileSz = (uint)sInfo.GetValue("fileSz", typeof(uint));
                uid = (ushort)sInfo.GetValue("uid", typeof(ushort));
                gid = (ushort)sInfo.GetValue("gid", typeof(ushort));
                chdate = (DateTime)sInfo.GetValue("chdate", typeof(DateTime));
                crdate = (DateTime)sInfo.GetValue("crdate", typeof(DateTime));
            }

            public void GetObjectData(SerializationInfo sInfo, StreamingContext contextArg)
            {
                sInfo.AddValue("id_inode", id_inode);
                sInfo.AddValue("perm", perm);
                sInfo.AddValue("flags", flags);
                sInfo.AddValue("fileSz", fileSz);
                sInfo.AddValue("uid", uid);
                sInfo.AddValue("gid", gid);
                sInfo.AddValue("chdate", chdate);
                sInfo.AddValue("crdate", crdate);
            }
        }

        /*Корневой каталог*/
        [Serializable]
        public class Root : ISerializable
        {
            public string name;
            public ushort idinode;

            public Root(string FileName, ushort IDinode)
            {
                name = FileName;
                idinode = IDinode;
            }

            public Root(SerializationInfo sInfo, StreamingContext contextArg)
            {
                idinode = (ushort)sInfo.GetValue("idinode", typeof(ushort));
                name = (string)sInfo.GetValue("name", typeof(string));
            }

            public void GetObjectData(SerializationInfo sInfo, StreamingContext contextArg)
            {
                sInfo.AddValue("idinode", idinode);
                sInfo.AddValue("name", name);
            }
        }

        [Serializable]
        public class SerializableRoot : ISerializable
        {
            private List<Root> roots;

            public List<Root> Roots
            {
                get { return roots; }
                set { roots = value; }
            }

            public SerializableRoot() { }

            public SerializableRoot(SerializationInfo sInfo, StreamingContext contextArg)
            {
                roots = (List<Root>)sInfo.GetValue("Roots", typeof(List<Root>));
            }

            public void GetObjectData(SerializationInfo sInfo, StreamingContext contextArg)
            {
                sInfo.AddValue("Roots", roots);
            }
        }

        public class RootSerializer
        {
            public RootSerializer() { }

            public void SerializableRoot(string fileName, SerializableRoot objToSerialize)
            {
                FileStream fstream = File.Open(fileName, FileMode.Create);
                BinaryFormatter binform = new BinaryFormatter();
                binform.Serialize(fstream, objToSerialize);
                fstream.Close();
            }

            public SerializableRoot DeserializeRoot(string fileName)
            {
                SerializableRoot objToSerialize = null;
                FileStream fstream = File.Open(fileName, FileMode.Open);
                BinaryFormatter binform = new BinaryFormatter();
                objToSerialize = (SerializableRoot)binform.Deserialize(fstream);
                fstream.Close();
                return objToSerialize;
            }
        }
    }
}