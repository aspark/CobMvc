FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["demo/CobMvc.Demo.Shop.ApiServer/CobMvc.Demo.Shop.ApiServer.csproj", "demo/CobMvc.Demo.Shop.ApiServer/"]
RUN dotnet restore "demo/CobMvc.Demo.Shop.ApiServer/CobMvc.Demo.Shop.ApiServer.csproj"
COPY . .
WORKDIR "/src/demo/CobMvc.Demo.Shop.ApiServer"
RUN dotnet build "CobMvc.Demo.Shop.ApiServer.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "CobMvc.Demo.Shop.ApiServer.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CobMvc.Demo.Shop.ApiServer.dll"]