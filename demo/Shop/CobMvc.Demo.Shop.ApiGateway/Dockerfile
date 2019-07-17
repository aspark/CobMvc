FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /
COPY ["demo/Shop/CobMvc.Demo.Shop.ApiGateway/CobMvc.Demo.Shop.ApiGateway.csproj", "demo/Shop/CobMvc.Demo.Shop.ApiGateway/"]
RUN dotnet restore "demo/Shop/CobMvc.Demo.Shop.ApiGateway/CobMvc.Demo.Shop.ApiGateway.csproj"
COPY . .
WORKDIR "demo/Shop/CobMvc.Demo.Shop.ApiGateway"
RUN dotnet build "CobMvc.Demo.Shop.ApiGateway.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "CobMvc.Demo.Shop.ApiGateway.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CobMvc.Demo.Shop.ApiGateway.dll"]