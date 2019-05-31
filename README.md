# Cobweb

原本打算采用[Orleans](https://github.com/dotnet/orleans)分布式框架的作为业务开发的基础的，但其Consul支持方式太过简单粗暴，也没有实现Consul主动的健康检查机制，首先自己尝试重写了Consul实现，并加入Service注册和健康检查机制，但奈何Orleans的TableMembership太过复杂，一直无法达到理想效果，最终只能放弃。后来将目光转移到[Surging](https://github.com/dotnetcore/surging)微服务框架，这是一个非常优秀的框架，只是自己不太喜欢侵入性太高的方式，所以Cobweb诞生了。

CobWeb基于asp..net core mvc开发，定位为一款简单、低侵入性的微服务框架。至于名称的由来是因为联想到微服务调用链想蜘蛛网一样，然后[iciba](http://www.iciba.com/)给出了这个单词～

> **注意：当前框架还处理快速迭代中，部分功能不稳定**

## 使用

### 服务注册与发现

#### 注册

在mvc `Startup`类添加MVC组件时`AddCobweb()`即可
```C#
services.AddMvc()
        .AddCobweb(cob=> {
            cob.ConfigureOptions(opt=> {
                opt.ServiceName = "CobwebDemo";
                //opt.ServiceAddress = "http://localhost:54469";//若为IIS，需要配置地址，否则Cobweb会自动从WebHost中获取
                opt.HealthCheck = "/api/test/Health";//不配置时，不做健康检查
            });
            cob.UseConsul(opt=> {//若不使用Consul，则默认使用InMemory方式
                opt.Address = new Uri("http://localhost:8500");
            });
        });
```

#### MVC调用其它服务
将`ICobClientFactory`由构造函数注入后直接调用即可
```C#
public TestController(ICobClientFactory clientFactory)
{
    _clientFactory = clientFactory;

    _clientFactory.GetProxy<IDemo>().GetNames();

    //效果同上，非接口实现方式的调用
    //GetProxy(new CobServiceDescriptor { ServiceName = "CobwebDemo" }).Invoke<string[]>("GetNames", null, null);
}
```

#### Console客户端调用
console需要自行构造`ServiceCollection`来启用`DI`，然后像mvc中一样`AddCobweb()`并进行相关配置即可。

```C#
var services = new ServiceCollection();

services.AddCobweb(cob => {
    cob.UseConsul(opt => {
        opt.Address = new Uri("http://localhost:8500");
    });
});

var provider = services.BuildServiceProvider();

var strs = provider.GetService<ICobClientFactory>().GetProxy<IDemo>().GetNames();//接口实现方式

//效果同上，非接口实现方式的调用
//var strs = provider.GetService<ICobClientFactory>().GetProxy(new CobServiceDescriptor { ServiceName = "CobwebDemo" }).Invoke<string[]>("GetNames", null, null);


Console.WriteLine("返回:{0}", string.Join(", ", strs));
```

#### 服务接口申明

为方便各系统调，可使用接口申明服务提供的api明细。该操作非必须，但在同一解决方案/业务内还是建议使用。
```C#
    [CobService("CobwebDemo", Path ="/api/test/")]
    //todo:重试、超时、负载等策略
    public interface IDemo
    {
        [CobService(Path = "/api/test/GetNames")]
        string[] GetNames();

        string[] GetOtherNames();

        Task<UserInfo> GetUserInfo(string name);


        Task<UserInfo> SaveUserInfo(UserInfo user);
    }
```

---

### Websocket
> todo

### Protobuf
> todo

### 统一配置 
> todo


### 统一日志
> todo
