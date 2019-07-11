# Shop Demo

## 概览
Shop Demo简单演示使用CobMvc快速搭建微服务系统的过程，Shop的功能用例如下：  
![shop demo 用例图](http://shop-case.png)  

ShopDemo只是简单的演示了前端系统，不包含后台管理系统、数据库、队列服务等。所以依据用用例图将系统划分为三个微服务：用户管理、商品管理和订单管理。整个系统使用前后端分离的模式，前端使用vuejs；后端使用asp.net core MVC+CobMVC的微服务架构，为解决web调用动态接口及鉴权等问题，使用Ocelot和CobMvc分别实现了两套网关(`CobMvc.Demo.Shop.ApiGateway` `CobMvc.Demo.Shop.ApiServer`)。系统组成如下：  
![shop demo 组件图](http://shop-components.png)  

因各微服务之间存在调用关系，所以使用接口将其抽象，以方便程序中使用强类型调用，并预留后面配置服务名和调用策略。设计类图如下：

![shop demo 类图](http://shop-class.png)

最后，各个微服务可独立运行在每个host/docker中，或共同在一个host中运行，他们相互之前的依赖调用关系由CobMvc管理，部署图如下：

![shope demo 部署图](http://shop-deployment.png)



## 实现



## Docker



## Kubernates