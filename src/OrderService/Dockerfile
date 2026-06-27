FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/OrderService/OrderService.csproj", "src/OrderService/"]
COPY ["shared/PedidoRapido.Contracts/PedidoRapido.Contracts.csproj", "shared/PedidoRapido.Contracts/"]
RUN dotnet restore "src/OrderService/OrderService.csproj"
COPY . .
WORKDIR "/src/src/OrderService"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]
