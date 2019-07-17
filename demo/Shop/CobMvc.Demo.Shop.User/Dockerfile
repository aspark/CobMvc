FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /
COPY ["demo/Shop/CobMvc.Demo.Shop.User/CobMvc.Demo.Shop.User.csproj", "demo/Shop/CobMvc.Demo.Shop.User/"]
RUN dotnet restore "demo/Shop/CobMvc.Demo.Shop.User/CobMvc.Demo.Shop.User.csproj"
COPY . .
WORKDIR "demo/Shop/CobMvc.Demo.Shop.User"
RUN dotnet build "CobMvc.Demo.Shop.User.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "CobMvc.Demo.Shop.User.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CobMvc.Demo.Shop.User.dll"]