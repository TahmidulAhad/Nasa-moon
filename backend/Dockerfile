# NasaMoon API — multi-stage Docker build (.NET 8)
# Build:  docker build -t nasamoon-api -f backend/Dockerfile backend
# Run:    docker run -e ConnectionStrings__DefaultConnection="Host=...;..." -p 5083:8080 nasamoon-api

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY NasaMoon.sln ./
COPY src/Domain/NasaMoon.Domain.csproj src/Domain/
COPY src/Application/NasaMoon.Application.csproj src/Application/
COPY src/Infrastructure/NasaMoon.Infrastructure.csproj src/Infrastructure/
COPY src/Api/NasaMoon.Api.csproj src/Api/
RUN dotnet restore
COPY . .
RUN dotnet publish src/Api/NasaMoon.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# Render injects $PORT; listen on it (default 8080 locally).
CMD ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet NasaMoon.Api.dll
