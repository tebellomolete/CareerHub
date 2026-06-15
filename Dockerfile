# ── Stage 1: Build ────────────────────────────────────────────────────────────
# The SDK image contains the compiler and build tools.
# This stage is discarded after build — nothing from it reaches production.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first and restore separately.
# Docker caches this layer — NuGet restore only re-runs when .csproj changes.
COPY ["API/API.csproj", "API/"]
RUN dotnet restore "API/API.csproj"

# Copy the rest of the source and publish.
COPY API/ API/
WORKDIR "/src/API"
RUN dotnet publish "API.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
# The ASP.NET runtime image — no compiler, no source.
# This is what runs in production.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# ASP.NET Core listens on 8080 by default in .NET 8+ containers.
EXPOSE 8080
ENTRYPOINT ["dotnet", "API.dll"]
