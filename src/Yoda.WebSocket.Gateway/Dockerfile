FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80 5000

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["src/Yoda.WebSocket.Gateway/Yoda.WebSocket.Gateway.csproj", "src/Yoda.WebSocket.Gateway/"]
COPY ["src/Yoda.WebSocket.Gateway.Core/Yoda.WebSocket.Gateway.Core.csproj", "src/Yoda.WebSocket.Gateway.Core/"]
RUN dotnet restore "src/Yoda.WebSocket.Gateway/Yoda.WebSocket.Gateway.csproj"
COPY . .
WORKDIR "/src/src/Yoda.WebSocket.Gateway"
RUN dotnet build "Yoda.WebSocket.Gateway.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Yoda.WebSocket.Gateway.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Yoda.WebSocket.Gateway.dll"]