FROM planetoidgenbase:1.0.3 AS base

WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["PlanetoidGen.Server/src/PlanetoidGen.API.AgentWorker/PlanetoidGen.API.AgentWorker.csproj", "PlanetoidGen.Server/src/PlanetoidGen.API.AgentWorker/"]
COPY ["PlanetoidGen.Server/src/PlanetoidGen.Infrastructure/PlanetoidGen.Infrastructure.csproj", "PlanetoidGen.Server/src/PlanetoidGen.Infrastructure/"]
COPY ["PlanetoidGen.Server/src/PlanetoidGen.DataAccess/PlanetoidGen.DataAccess.csproj", "PlanetoidGen.Server/src/PlanetoidGen.DataAccess/"]
COPY ["PlanetoidGen.Core/src/PlanetoidGen.BusinessLogic/PlanetoidGen.BusinessLogic.csproj", "PlanetoidGen.Core/src/PlanetoidGen.BusinessLogic/"]
COPY ["PlanetoidGen.Core/src/PlanetoidGen.BusinessLogic.Agents/PlanetoidGen.BusinessLogic.Agents.csproj", "PlanetoidGen.Core/src/PlanetoidGen.BusinessLogic.Agents/"]
COPY ["PlanetoidGen.Core/src/PlanetoidGen.BusinessLogic.Common/PlanetoidGen.BusinessLogic.Common.csproj", "PlanetoidGen.Core/src/PlanetoidGen.BusinessLogic.Common/"]
COPY ["PlanetoidGen.Core/src/PlanetoidGen.Agents.Standard/PlanetoidGen.Agents.Standard.csproj", "PlanetoidGen.Core/src/PlanetoidGen.Agents.Standard/"]
COPY ["PlanetoidGen.Core/src/PlanetoidGen.Contracts/PlanetoidGen.Contracts.csproj", "PlanetoidGen.Core/src/PlanetoidGen.Contracts/"]
COPY ["PlanetoidGen.Core/src/PlanetoidGen.Domain/PlanetoidGen.Domain.csproj", "PlanetoidGen.Core/src/PlanetoidGen.Domain/"]
COPY ["PlanetoidGen.Core/src/PlanetoidGen.Agents.Osm/PlanetoidGen.Agents.Osm.csproj", "PlanetoidGen.Core/src/PlanetoidGen.Agents.Osm/"]
COPY ["PlanetoidGen.Core/src/PlanetoidGen.Agents.Procedural/PlanetoidGen.Agents.Procedural.csproj", "PlanetoidGen.Core/src/PlanetoidGen.Agents.Procedural/"]
COPY ["PlanetoidGen.Core/src/PlanetoidGen.Agents.Standard/PlanetoidGen.Agents.Standard.csproj", "PlanetoidGen.Core/src/PlanetoidGen.Agents.Standard/"]

RUN dotnet restore "PlanetoidGen.Server/src/PlanetoidGen.API.AgentWorker/PlanetoidGen.API.AgentWorker.csproj"

COPY . .

WORKDIR "PlanetoidGen.Server/src/PlanetoidGen.API.AgentWorker"
RUN dotnet build "PlanetoidGen.API.AgentWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PlanetoidGen.API.AgentWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN cp /lib/x86_64-linux-gnu/libdl.so.2 /app/liblibdl.so

ENTRYPOINT ["dotnet", "PlanetoidGen.API.AgentWorker.dll"]
