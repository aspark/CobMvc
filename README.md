# CobMvc

原本打算采用[Orleans](https://github.com/dotnet/orleans)分布式框架的作为业务开发的基础的，但其Consul支持方式太过简单粗暴，也没有实现Consul主动的健康检查机制，首先自己尝试重写了Consul实现，并加入Service注册和健康检查机制，但奈何Orleans的TableMembership太过复杂，一直无法达到理想效果，最终只能放弃。后来将目光转移到[Surging](https://github.com/dotnetcore/surging)微服务框架，这是一个非常优秀的框架，只是自己不太喜欢侵入性太高的方式，所以CobMvc诞生了。

CobMvc基于asp..net core mvc开发，定位为一款简单、低侵入性的微服务框架。至于名称的由来是因为联想到微服务调用链想蜘蛛网一样，然后[iciba](http://www.iciba.com/)给出了这个单词~~Cobweb~~(在nuget被占用，所以改为CobMvc了)～

> **注意：当前框架还处理快速迭代中，部分功能不稳定**

## 使用

```
PM> Install-Package CobMvc -Version 0.0.1-alpha
```

### 服务注册与发现

#### 注册

1. 添加CobMvc引用后, 在mvc `Startup.ConfigureServices(IServiceCollection services)`加MVC组件时一并添用`AddCobMvc()`
```C#
services.AddMvc()
        .AddCobMvc(cob=> {
            cob.ConfigureOptions(opt=> {
                opt.ServiceName = "CobMvcDemo";
                //opt.ServiceAddress = "http://localhost:54469";//若为IIS，需要配置地址，否则CobMvc会自动从WebHost中获取
                opt.HealthCheck = "/api/test/Health";//不配置时，不做健康检查
            });
            cob.AddConsul(opt=> {//若不使用Consul，则默认使用InMemory方式
                opt.Address = new Uri("http://localhost:8500");
            });
        });
```

2. 在`Configure(IApplicationBuilder app, IHostingEnvironment env)`中调用`app.UseCobMvc();`将当前asp.net mvc服务添加到注册中心。如当前项目只须调用其它服务，可不使用该方法

#### 1. 在asp.net mvc内调用其它服务
将`ICobClientFactory`由构造函数注入后直接调用即可
```C#
public TestController(ICobClientFactory clientFactory)
{
    _clientFactory = clientFactory;

    _clientFactory.GetProxy<IDemo>().GetNames();

    //效果同上，非接口实现方式的调用
    //GetProxy(new CobServiceDescriptor { ServiceName = "CobMvcDemo" }).Invoke<string[]>("GetNames", null, null);
}
```

#### 2. 在Console客户端调用其它服务
console需要自行构造`ServiceCollection`来启用`DI`，然后像mvc中一样`AddCobMvc()`并进行相关配置即可。

```C#
var services = new ServiceCollection();

services.AddCobMvc(cob => {
    cob.AddConsul(opt => {
        opt.Address = new Uri("http://localhost:8500");
    });
});

var provider = services.BuildServiceProvider();

var strs = provider.GetService<ICobClientFactory>().GetProxy<IDemo>().GetNames();//接口实现方式

//效果同上，非接口实现方式的调用
//var strs = provider.GetService<ICobClientFactory>().GetProxy(new CobServiceDescriptor { ServiceName = "CobMvcDemo" }).Invoke<string[]>("GetNames", null, null);


Console.WriteLine("返回:{0}", string.Join(", ", strs));
```

#### 采用接口申明服务

为方便各系统调，可使用接口申明服务提供的api明细。该操作非必须，但在同一解决方案/业务内还是建议使用。
```C#
    [CobService("CobMvcDemo", Path ="/api/test/", Transport = CobRequestTransports.WebSocket, Timeout = 1)]//使用Websocket通讯，接口调用1秒超时，
    [CobRetryStrategy(Count = 3, Exceptions = new[] { typeof(TimeoutException), typeof(Exception) })]//发生超时后，重试次数
    public interface IDemo
    {
        [CobService(Path = "/api/test/GetNames")]
        string[] GetNames();

        [CobService(Transport = CobRequestTransports.Http)]//该方法使用http
        string[] GetOtherNames();

        Task<UserInfo> GetUserInfo(string name);


        Task<UserInfo> SaveUserInfo(UserInfo user);
        
        void Mark(int ms);

        [CobRetryStrategy(FallbackValue = "new string[1]{\"default\"}")]//服务调用失败后，返回默认值
        string[] Fallback();
    }
```

---


### 使用Websocket
引用CobMvc.WebSockets项目并添加WebSockets支持：
```C#
 services.AddMvc()
    .AddCobMvc(cob=> {
        cob.AddCobWebSockets();
    });
```
然后在`Configure(IApplicationBuilder app, IHostingEnvironment env)`中通过`app.UseCobWebSockets();`启用WebSockets

---

### Protobuf
> todo


### 统一配置 
引用CobMvc.Consul.Configuration项目，该项目是基于consul kv统一分发配置。兼容asp.net core 原生`IConfiguration`方式，该库可单独使用。使用方式如下：
```C#
builder.ConfigureAppConfiguration(b=> {
    b.AddJsonFile("appsettings.json");//添加本地json默认配置(可选)
    b.AddConsul(consul => {
        consul.Address = new Uri("http://localhost:8500");
    });
})
```

示例代码对应的consul kv值配置如下(string与json混用)：
``` 
    CobMvc/Configuration/current/db="value"
    CobMvc/Configuration/current/auth={token:3, expired:"01:00:00"}

```

以上kv对应的等效json如下：
``` json
{
  "current": {
    "db": "value",
    "auth": { "token": 3, expired:"01:00:00" }
  }
}
```

#### 1. 使用Configuration方式获取配置 
```C#
    var setting = new Settings();
    config.GetSection("current");//config:asp.net mvc中注入的IConfiguration
    config.Bind("current", setting);
```

#### 2. 使用Options方式方式获取配置
在`StartUp`中使用`services.Configure<Settings>(config.GetSection("current"))`进行配置后，就可以将`IOptions<Settings>`/`IOptionsMonitor<Settings>`等注入到需要使用的地方

---

### 统一日志
> todo
