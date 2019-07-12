# Shop Demo

## 概览
Shop Demo简单演示使用CobMvc快速搭建微服务系统的过程，Shop的功能用例如下：  
![shop demo 用例图](https://raw.githubusercontent.com/aspark/CobMvc/dev/tutorials/Images/shop-case.png)  


ShopDemo只是简单的演示了前台系统，不包含后台管理系统、数据库、队列服务等。所以依据用例图/领域将系统划分为三个微服务：用户管理、商品管理和订单管理。整个系统使用前后端分离的模式，前端使用[Vuejs](https://vuejs.org)；后端使用asp .net core MVC+CobMVC的微服务架构，为解决web调用动态接口及鉴权等问题，使用Ocelot和CobMvc分别实现了两套网关(`CobMvc.Demo.Shop.ApiGateway`和`CobMvc.Demo.Shop.ApiServer`)。系统组件划分如下：  
![shop demo 组件图](https://raw.githubusercontent.com/aspark/CobMvc/dev/tutorials/Images/shop-components.png)  


因各微服务之间存在调用关系，所以使用接口将其抽象，以方便程序中使用强类型调用，并通过接口配置服务名和调用策略。设计类图如下：  
![shop demo 类图](https://raw.githubusercontent.com/aspark/CobMvc/dev/tutorials/Images/shop-class.png)


最后，shop demo可运行于windows/linux/macOS中，各个微服务可独立分布在每个host/docker中，或共同在一个host中运行。他们相互之前的依赖调用关系由CobMvc管理，部署图如下：  
![shope demo 部署图](https://raw.githubusercontent.com/aspark/CobMvc/dev/tutorials/Images/shop-deployment.png)

## 运行Demo
1. Shop Demo使用的Consul作为服务注册发布中心，所以请先安装并启动Consul（单机下可使用`Consul agent -dev`执行，启用8500端号）
2. 编译Shop Demo。示例使用的是项目依赖CobMvc，也自行改为Nuget安装包，详见：https://www.nuget.org/packages?q=aspark
3. 服务启用脚本放置于`\demo\Shop\CobMvc.Demo.Shop.Starter`目录中（linux下命令是一样的）编译成功后直接执行即可，各脚本含义如下：

|文件名|说明|
|---|---|
|start api(cob)+5000.bat|使用5000端口，启动CobMVC实现的网关|
|start api(ocelot)+5000.bat|使用5000端口，启动Ocelot实现的网关|
|start order+5001.bat|使用5001端口，启动订单管理服务|
|start order+5011.bat|使用5011端口，启动订单管理服务|
|start product+5002.bat|使用5002端口，启动商品管理服务|
|start user+5003.bat|使用5003端口，启动用户管理服务|
|start user+5013.bat|使用5003端口，启动用户管理服务|
>所有服务启动成功后，访问http://localhost:5000/gw/vapi/index 正常返回合并后的json   
> ocelot网关中`/gw/vapi/index`接口是通过配置将用户管理和商品管理两个接口合二为一，示例中没有额外配置ocelot异常服务处理，所以有存在30s异常切换时间

4. 前端Vuejs项目放置于`\demo\Shop\CobMvc.Demo.Shop.H5`中，使用`npm install`还原所有包后，执行`npm run serve`启动服务（开发模式）
5. 最后访问`http://localhost:8082/`即可开始使用：
![shop demo效果](https://raw.githubusercontent.com/aspark/CobMvc/dev/tutorials/Images/shop.gif)

## 实现
以商品管理服务为例
1. 新建asp.net core mvc项目并命名为`CobMvc.Demo.Shop.Product`，添加CobMvc引用后，在`StartUp`类中通过以下代码启用CobMvc服务注册与发现：
```C#
services.AddMvc().AddCobMvc(cob => {
    cob.AddConsul(config => {
        config.Address = new Uri(Configuration.GetValue<string>("Consul:Address"));//现用Consul作为服务注册中心
    });
});
```
```C#
    app.UseCobMvc(opts => {
        opts.ServiceName = "CobMvc.Demo.Shop.Product";
        //opts.ServiceAddress = "";
        opts.HealthCheck = "/api/product/check";
    });
```
2. 在公共类库`CobMvc.Demo.Shop.Contract`中添加接口`IProduct`用于服务声明。使用`CobService`配置服务名为`CobMvc.Demo.Shop.Product`及调用根路径为`/api/product`；其中针对库存检查方法`CheckStock`，使用`CobRetryStrategy`配置为在发生异常时重试3次，如果最终还是失败，则使用`CheckStockFallbackHandler`返回默认值`false`

```C#
    [CobService("CobMvc.Demo.Shop.Product", Path = "/api/product")]
    public interface IProduct
    {
        Task<ApiResult<ProductDto[]>> GetProducts();

        [CobRetryStrategy(Count = 3, FallbackHandler = typeof(CheckStockFallbackHandler))]//FallbackValue = "Task.FromResult(ApiResult.Create<bool>(false))"
        Task<ApiResult<bool>> CheckStock(Guid productID);
    }
```

3. 修改`ProductController`并实现`IProduct`接口后，就完成了商品管理服务的开发。
4. 对于服务之间的依赖，在构造函数中注入`ICobClientFactory clientFactory`变量后，在需要调用其它服务的地方，如`OrderController.CreateOrder()`中创建订单时需要调用商品管理服务的存存检查，则可以这样：
```C#
    var stock = await _clientFactory.GetProxy<IProduct>().CheckStock(item.ProductID);
```

## Docker
1. 拉取Consul镜像 `docker pull consul:latest`

2. 运行Consul `docker run -d --name=dev-consul -e CONSUL_BIND_INTERFACE=eth0 consul`

> 以下命令在CobMvc根目录下执行,以`CobMvc.Demo.Shop.Product`为例

3. 使用`docker build -t product -f demo/shop/CobMvc.Demo.Shop.Product/Dockerfile ./`编译镜像

4. 使用`docker images`查看镜像名为`product`/`94c244670461`

5. 使用`docker run -it -e "Consul__Address=http://172.17.0.2:8500" 94c244670461`配置Consul并执行`CobMvc.Demo.Shop.Product`服务，成功后显示如下：
![shop demo product服务](https://raw.githubusercontent.com/aspark/CobMvc/dev/tutorials/Images/shop-docker-run-product.png)

6. 使用`curl http://172.17.0.5/api/product/GetProducts` 检查是否运行成功 因hyperV下无法ssh linux vm，以下为在alpine容器中使用wget的测试结果：
![shop demo product api](https://raw.githubusercontent.com/aspark/CobMvc/dev/tutorials/Images/shop-docker-run-product-api.png)

7. 使用 `curl http://172.17.0.2:8500/v1/agent/services`查看Consul中注册的服务如下：
![shop demo product api](https://raw.githubusercontent.com/aspark/CobMvc/dev/tutorials/Images/shop-docker-run-consul-services.png)


> 运行网关等其它服务与上面方法的类似，docker网络置配以实际环境为准

## Kubernates

> //todo: 基于上面Docker的使用，添加Pod和service暴露网关。后续可以引用`CobMvc.Kubernetes`包使用NameService替换Consul