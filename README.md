# UODisc
A discord client for ServUO

---

Installation:

1. Download this repository and put it into your custom server folder
2. Open Server/Server.csproj
3. Look for: <ItemGroup>
4. Below it insert:

<PackageReference Include="DSharpPlus" Version="3.2.3" />
<PackageReference Include="DSharpPlus.WebSocket.WebSocket4Net" Version="3.2.3" />
<PackageReference Include="DSharpPlus.WebSocket.WebSocketSharp" Version="3.2.3" />

5. In the base server folder run the following command: dotnet restore
6. Recompile the server
7. Done