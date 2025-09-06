# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution file and project files
COPY *.sln ./
COPY Shortly.API/Shortly.API.csproj ./Shortly.API/
COPY Shortly.Core/Shortly.Core.csproj ./Shortly.Core/
COPY Shortly.Domain/Shortly.Domain.csproj ./Shortly.Domain/
COPY Shortly.Infrastructure/Shortly.Infrastructure.csproj ./Shortly.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . ./

# Build the application
RUN dotnet publish Shortly.API/Shortly.API.csproj -c Release -o out

# Use the official .NET 8 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/out .

# Expose port 80 and 443
EXPOSE 80
EXPOSE 443

# Set the entry point
ENTRYPOINT ["dotnet", "Shortly.API.dll"]