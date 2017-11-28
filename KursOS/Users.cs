using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace KursOS
{
    [Serializable]
    public class Users :ISerializable
    {
        public ushort uid;
        public string login;
        public string password;

        public Users(ushort UID, string Login, string Password)
        {
            uid = UID;
            login = Login;
            password = Password;
            //File.AppendAllText("../../UsrList.sys", "[" + Login + "]\r" + Password + "\r");
        }
        
        public Users(SerializationInfo sInfo, StreamingContext contextArg)
        {
            uid = (ushort)sInfo.GetValue("uid", typeof(ushort));
            login = (string)sInfo.GetValue("login", typeof(string));
            password = (string)sInfo.GetValue("password", typeof(string));
        }

        public void GetObjectData(SerializationInfo sInfo, StreamingContext contextArg)
        {
            sInfo.AddValue("uid", uid);
            sInfo.AddValue("login", login);
            sInfo.AddValue("password", password);
        }

        [Serializable]
        public class SerializableUsers : ISerializable
        {
            private List<Users> users;

            public List<Users> Users
            {
                get { return users; }
                set { users = value; }
            }

            public SerializableUsers() { }

            public SerializableUsers(SerializationInfo sInfo, StreamingContext contextArg)
            {
                sInfo.AddValue("Users", users);
            }

            public void GetObjectData(SerializationInfo sInfo, StreamingContext contextArg)
            {
                users = (List<Users>)sInfo.GetValue("Users", typeof(List<Users>));
            }
        }

        public class UsersSerializer
        {
            public UsersSerializer() { }

            public void SerializeUsers(string fileName, SerializableUsers objToSerialize)
            {
                FileStream fstream = File.Open(fileName, FileMode.Create);
                BinaryFormatter binform = new BinaryFormatter();
                binform.Serialize(fstream, objToSerialize);
                fstream.Close();
            }

            public SerializableUsers DeserializeUsers(string fileName)
            {
                SerializableUsers objToSerialize = null;
                FileStream fstream = File.Open(fileName, FileMode.Open);
                BinaryFormatter binform = new BinaryFormatter();
                objToSerialize = (SerializableUsers)binform.Deserialize(fstream);
                fstream.Close();
                return objToSerialize;
            }
        }
    }
}
