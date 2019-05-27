using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb.ClientDemo
{
    [CobClient]
    public interface IDemo
    {
        Task<UserInfo> GetUserInfo(string name);
    }

    public class UserInfo
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public string Addr { get; set; }
    }
}
