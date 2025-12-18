FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

# Install Node.js and npm for frontend build
RUN apt-get update && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Copy dependency files first for better cache utilization
COPY Directory.Build.props .
COPY RSSAIzer.Shared/RSSAIzer.Shared.csproj RSSAIzer.Shared/
COPY RSSAIzer.Backend/RSSAIzer.Backend.csproj RSSAIzer.Backend/
COPY RSSAIzer.Web/RSSAIzer.Web.csproj RSSAIzer.Web/
COPY RSSAIzer.Web/package.json RSSAIzer.Web/package-lock.json RSSAIzer.Web/

# Restore npm dependencies (cached unless package files change)
WORKDIR /src/RSSAIzer.Web
RUN npm ci

# Restore .NET dependencies (cached unless .csproj files change)
WORKDIR /src
RUN dotnet restore "RSSAIzer.Web/RSSAIzer.Web.csproj"

# Copy remaining source code (this layer invalidates less frequently)
COPY . .

# Build and publish .NET projects
RUN dotnet build "RSSAIzer.Web/RSSAIzer.Web.csproj" -c $BUILD_CONFIGURATION --no-restore
RUN dotnet publish "RSSAIzer.Web/RSSAIzer.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5110
ENTRYPOINT ["dotnet", "RSSAIzer.Web.dll"]
