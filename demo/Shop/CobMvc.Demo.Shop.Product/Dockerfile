FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /
COPY ["demo/CobMvc.Demo.Shop.Product/CobMvc.Demo.Shop.Product.csproj", "demo/CobMvc.Demo.Shop.Product/"]
RUN dotnet restore "demo/CobMvc.Demo.Shop.Product/CobMvc.Demo.Shop.Product.csproj"
COPY . .
WORKDIR "demo/CobMvc.Demo.Shop.Product"
RUN dotnet build "CobMvc.Demo.Shop.Product.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "CobMvc.Demo.Shop.Product.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CobMvc.Demo.Shop.Product.dll"]