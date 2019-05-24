using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cobweb.Core.Service
{
    public interface IServiceRegistration
    {
        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        Task<bool> Register(ServiceInfo entry);

        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> Deregister(string id);

        /// <summary>
        /// 获取服务列表
        /// </summary>
        /// <returns></returns>
        Task<List<ServiceInfo>> GetAll();

        /// <summary>
        /// 获取服务列表
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<List<ServiceInfo>> GetByName(string name);
    }
}
