FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY apiguard.slnx .
COPY src/ApiGuard.Core/ApiGuard.Core.csproj src/ApiGuard.Core/
COPY src/ApiGuard.Cli/ApiGuard.Cli.csproj src/ApiGuard.Cli/
RUN dotnet restore src/ApiGuard.Cli/ApiGuard.Cli.csproj

COPY src/ src/
RUN dotnet publish src/ApiGuard.Cli/ApiGuard.Cli.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "/app/ApiGuard.Cli.dll"]
