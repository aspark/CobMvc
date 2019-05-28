using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb.Demo.Contract
{
    public interface IDemo
    {
        string[] GetNames();

        Task<UserInfo> GetUserInfo(string name);
    }

    public class UserInfo
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public string Addr { get; set; }
    }
}
