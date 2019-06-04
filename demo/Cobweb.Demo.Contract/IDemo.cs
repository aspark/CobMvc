using Cobweb.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb.Demo.Contract
{
    [CobService("CobwebDemo", Path ="/api/test/")]
    public interface IDemo
    {
        [CobService(Path = "/api/GetNames")]
        string[] GetNames();

        string[] GetOtherNames();

        Task<UserInfo> GetUserInfo(string name);


        Task SaveUserInfo(UserInfo user);

        void Mark(int ms);
    }

    public class UserInfo
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public string Addr { get; set; }
    }
}
