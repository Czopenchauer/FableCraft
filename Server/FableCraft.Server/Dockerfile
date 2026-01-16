FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY Server/FableCraft.Server/FableCraft.Server.csproj Server/FableCraft.Server/
COPY Server/FableCraft.Application/FableCraft.Application.csproj Server/FableCraft.Application/
COPY Server/FableCraft.Infrastructure/FableCraft.Infrastructure.csproj Server/FableCraft.Infrastructure/
COPY Server/FableCraft.ServiceDefaults/FableCraft.ServiceDefaults.csproj Server/FableCraft.ServiceDefaults/

WORKDIR /src/Server/FableCraft.Server
RUN dotnet restore -p:BuildingInDocker=true

WORKDIR /src
COPY Server/FableCraft.Server/ Server/FableCraft.Server/
COPY Server/FableCraft.Application/ Server/FableCraft.Application/
COPY Server/FableCraft.Infrastructure/ Server/FableCraft.Infrastructure/
COPY Server/FableCraft.ServiceDefaults/ Server/FableCraft.ServiceDefaults/

WORKDIR /src/Server/FableCraft.Server
RUN dotnet publish -c Release -o /app/publish --no-restore -p:BuildingInDocker=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/data-store /app/logs /app/prompts-root

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "FableCraft.Server.dll"]
