# Shop Demo

## 概览
Shop Demo简单演示使用CobMvc快速搭建微服务系统的过程，Shop的功能用例如下：  
![shop demo 用例图](https://raw.githubusercontent.com/aspark/CobMvc/dev/tutorials/Images/shop-case.png)  


ShopDemo只是简单的演示了前端系统，不包含后台管理系统、数据库、队列服务等。所以依据用用例图将系统划分为三个微服务：用户管理、商品管理和订单管理。整个系统使用前后端分离的模式，前端使用vuejs；后端使用asp.net core MVC+CobMVC的微服务架构，为解决web调用动态接口及鉴权等问题，使用Ocelot和CobMvc分别实现了两套网关(`CobMvc.Demo.Shop.ApiGateway` `CobMvc.Demo.Shop.ApiServer`)。系统组成如下：  
![shop demo 组件图](https://raw.githubusercontent.com/aspark/CobMvc/dev/tutorials/Images/shop-components.png)  


因各微服务之间存在调用关系，所以使用接口将其抽象，以方便程序中使用强类型调用，并预留后面配置服务名和调用策略。设计类图如下：  
![shop demo 类图](https://raw.githubusercontent.com/aspark/CobMvc/dev/tutorials/Images/shop-class.png)


最后，各个微服务可独立运行在每个host/docker中，或共同在一个host中运行，他们相互之前的依赖调用关系由CobMvc管理，部署图如下：  
![shope demo 部署图](https://raw.githubusercontent.com/aspark/CobMvc/dev/tutorials/Images/shop-deployment.png)

## 运行Demo
1. Shop Demo使用的Consul作为服务注册发布中心，所以请先安装并启动Consul（单机下可使用`Consul agent -dev`）
2. 服务启用脚本放置于`\demo\Shop\CobMvc.Demo.Shop.Starter`目录中（linux下命令是一样的）编译成功后直接执行即可，各脚本含义如下：

|文件名|说明|
|---|---|
|start api(cob)+5000.bat|使用5000端口，启动CobMVC实现的网关|
|start api(ocelot)+5000.bat|使用5000端口，启动Ocelot实现的网关,其中`/gw/vapi/index`接口是通过配置将用户管理和商品管理两个接口合二为一|
|start order+5001.bat|使用5001端口，启动订单管理服务|
|start order+5011.bat|使用5011端口，启动订单管理服务|
|start product+5002.bat|使用5002端口，启动商品管理服务|
|start user+5003.bat|使用5003端口，启动用户管理服务|
|start user+5013.bat|使用5003端口，启动用户管理服务|

3. 前端Vuejs项目放置于`\demo\Shop\CobMvc.Demo.Shop.H5`中，使用`npm install`还原所有包后，执行`npm run serve`启动服务（开发模式）
4. 最后访问`http://localhost:8082/`即可：
![shop demo效果]()

## 实现
以商品管理服务为列
1. 新建asp.net core mvc项目`CobMvc.Demo.Shop.Product`，添加CobMvc引用后，在`StartUp`类中通过以下代码启用CobMvc服务注册与发现：
```C#
services.AddMvc().AddCobMvc(cob => {
    cob.AddConsul(config => {
        config.Address = new Uri(Configuration.GetValue<string>("Consul:Address"));
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
2. 在公共类库`CobMvc.Demo.Shop.Contract`中添加添加接口`IProduct`，用于服务声明。使用`CobService`配置服务名为`CobMvc.Demo.Shop.Product`，调用根路径为`/api/product`；其中对库存检查方法`CheckStock`使用`CobRetryStrategy`配置为在发生异常时重试3次，如果最终还是失败，则使用`CheckStockFallbackHandler`返回默认值`false`

```C#
    [CobService("CobMvc.Demo.Shop.Product", Path = "/api/product")]
    public interface IProduct
    {
        Task<ApiResult<ProductDto[]>> GetProducts();

        [CobRetryStrategy(Count = 3, FallbackHandler = typeof(CheckStockFallbackHandler))]//FallbackValue = "Task.FromResult(ApiResult.Create<bool>(false))"
        Task<ApiResult<bool>> CheckStock(Guid productID);
    }
```

3. 修改`ProductController`实现`IProduct`就完成了商品管理服务的开发。
4. 对于服务之间的依赖，在构造函数注入`ICobClientFactory clientFactory`变量后，在需要调用其它服务的地方，如`OrderController.CreateOrder()`中需要调用商品管理服务的困存检查，则可以这样：
```C#
    var stock = await _clientFactory.GetProxy<IProduct>().CheckStock(item.ProductID);
```

## Docker



## Kubernates