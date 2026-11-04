# ---- build ---------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore from the project files only, so this layer is cached until a dependency changes.
# The API project is restored directly (not the solution, which also lists the test projects).
# Paths are case-sensitive on Linux: they must match the files on disk exactly.
COPY src/Shortly.Api/Shortly.API.csproj src/Shortly.Api/
COPY src/Shortly.Core/Shortly.Core.csproj src/Shortly.Core/
COPY src/Shortly.Domain/Shortly.Domain.csproj src/Shortly.Domain/
COPY src/Shortly.Infrastructure/Shortly.Infrastructure.csproj src/Shortly.Infrastructure/
RUN dotnet restore src/Shortly.Api/Shortly.API.csproj

COPY src/ src/
RUN dotnet publish src/Shortly.Api/Shortly.API.csproj -c Release -o /app/out --no-restore

# ---- runtime -------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# The .NET 8 runtime image listens on 8080 by default (ASPNETCORE_HTTP_PORTS). TLS is expected to be
# terminated by the hosting platform's proxy; see docs/deployment.md for the forwarded-header settings.
EXPOSE 8080

# No ASPNETCORE_ENVIRONMENT is set: the image runs as Production (no Swagger, no automatic migrations).
ENTRYPOINT ["dotnet", "Shortly.API.dll"]
