FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/NotificationService/NotificationService.csproj", "src/NotificationService/"]
COPY ["shared/PedidoRapido.Contracts/PedidoRapido.Contracts.csproj", "shared/PedidoRapido.Contracts/"]
RUN dotnet restore "src/NotificationService/NotificationService.csproj"
COPY . .
WORKDIR "/src/src/NotificationService"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
