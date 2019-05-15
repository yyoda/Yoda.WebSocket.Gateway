FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["sample/Backend.Server/Backend.Server.csproj", "sample/Backend.Server/"]
COPY ["src/Yoda.WebSocket.Gateway.Core/Yoda.WebSocket.Gateway.Core.csproj", "src/Yoda.WebSocket.Gateway.Core/"]
RUN dotnet restore "sample/Backend.Server/Backend.Server.csproj"
COPY . .
WORKDIR "/src/sample/Backend.Server"
RUN dotnet build "Backend.Server.csproj" -c ${BUILD_CONFIGURATION} \
 && dotnet publish "Backend.Server.csproj" -c ${BUILD_CONFIGURATION} -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 80 5001
ENTRYPOINT ["dotnet", "Backend.Server.dll"]
