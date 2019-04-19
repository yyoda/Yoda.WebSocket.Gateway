# NOTE


```cmd
dotnet publish ./src/Yoda.WebSocket.Gateway/Yoda.WebSocket.Gateway.csproj -c Release -o ../../bin/gateway
dotnet ./bin/gateway/Yoda.WebSocket.Gateway.dll

dotnet publish ./sample/Backend.Server/Backend.Server.csproj -c Release -o ../../bin/backend
dotnet ./bin/backend/Backend.Server.dll
```
