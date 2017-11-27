using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KursOS
{
    class Groups
    {
        public ushort gid;
        public List<Users> users_list = new List<Users>();

        public Groups(ushort GID)
        {
            gid = GID;
        }
        public bool AddUser(Users usr)
        {
            if (users_list.Contains(usr))
                return false;
            else
            {
                users_list.Add(usr);
                return true;
            }
        }
        public bool RemUser(ushort UserID)
        {
            for (int i = 0; i < users_list.Count; i++)
            {
                if (users_list[i].uid == UserID)
                {
                    users_list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
}
