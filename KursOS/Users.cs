using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KursOS
{
    class Users
    {
        public ushort uid;
        public string login;
        public string password;

        public Users(ushort UID, string Login, string Password)
        {
            uid = UID;
            login = Login;
            password = Password;
            File.AppendAllText("../../UsrList.sys", "\r[" + Login + "]\r" + Password);
        }

        private void CreateFile(string FileName, string path)
        {

        }
    }
}
