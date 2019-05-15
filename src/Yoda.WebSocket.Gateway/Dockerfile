FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Yoda.WebSocket.Gateway/Yoda.WebSocket.Gateway.csproj", "src/Yoda.WebSocket.Gateway/"]
COPY ["src/Yoda.WebSocket.Gateway.Core/Yoda.WebSocket.Gateway.Core.csproj", "src/Yoda.WebSocket.Gateway.Core/"]
RUN dotnet restore "src/Yoda.WebSocket.Gateway/Yoda.WebSocket.Gateway.csproj"
COPY . .
WORKDIR "/src/src/Yoda.WebSocket.Gateway"
RUN dotnet build "Yoda.WebSocket.Gateway.csproj" -c ${BUILD_CONFIGURATION} \
 && dotnet publish "Yoda.WebSocket.Gateway.csproj" -c ${BUILD_CONFIGURATION} -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 80 5000
ENTRYPOINT ["dotnet", "Yoda.WebSocket.Gateway.dll"]
