using CobMvc.Core;
using CobMvc.Core.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc.Demo.Contract
{
    [CobService("CobMvcDemo", Path ="/api/test/")]//, Timeout = 1, Transport = CobRequestTransports.WebSocket
    [CobRetryStrategy(Count = 5, Exceptions = new[] { typeof(TimeoutException), typeof(Exception) })]
    [AuthRequestFilter]
    public interface IDemo
    {
        [CobService(Path = "/api/GetNames")]
        string[] GetNames();

        [CobService(Transport = CobRequestTransports.Http)]
        string[] GetOtherNames();

        Task<UserInfo> GetUserInfo(string name);

        //
        Task SaveUserInfo(int operatorID, UserInfo user);

        //
        Task SaveUserInfo2(int operatorID, UserInfo user1, UserInfo user2);

        void Mark(int ms);

        [CobRetryStrategy(FallbackValue = "new string[1]{\"default\"}")]
        string[] Fallback();
  }

    public class UserInfo
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public string Addr { get; set; }
    }

    /// <summary>
    /// 模拟添加token校验
    /// </summary>
    public class AuthRequestFilter: CobRequestFilterAttribute
    {
        public override void OnBeforeRequest(CobRequestContext context)
        {
            //base.OnBeforeRequest(context);
            context.Parameters["t"] = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            context.Extensions["x-token"] = "token-abc";
        }
    }
}
