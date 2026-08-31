# Step 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for caching package restore
COPY ExchangeRate.Domain/ExchangeRate.Domain.csproj ExchangeRate.Domain/
COPY ExchangeRate.Application/ExchangeRate.Application.csproj ExchangeRate.Application/
COPY ExchangeRate.Infrastructure/ExchangeRate.Infrastructure.csproj ExchangeRate.Infrastructure/
COPY ExchangeRate.Api/ExchangeRate.Api.csproj ExchangeRate.Api/

# Restore dependencies for the API project (which transitively restores all referenced projects)
RUN dotnet restore ExchangeRate.Api/ExchangeRate.Api.csproj

# Copy the remaining source files
COPY . .

# Build and publish the Web API startup project in Release mode
WORKDIR /src/ExchangeRate.Api
RUN dotnet publish ExchangeRate.Api.csproj -c Release -o /app/publish --no-restore

# Step 2: Final runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV DOTNET_USE_POLLING_FILE_WATCHER=1

# Copy published files from build stage
COPY --from=build /app/publish .

# Run the API application on the port dynamically assigned by Render (defaulting to 8080 if not set)
ENTRYPOINT ["sh", "-c", "dotnet ExchangeRate.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
