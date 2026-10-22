FROM scratch AS warm-nuget

FROM node:24-bookworm-slim AS web
WORKDIR /src
COPY apps/web/package*.json ./
RUN --mount=type=secret,id=platform-ca if [ -f /run/secrets/platform-ca ]; then export NODE_EXTRA_CA_CERTS=/run/secrets/platform-ca; fi; npm ci --no-audit --no-fund
COPY apps/web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY apps/api/SaigonGuide.Api.csproj apps/api/packages.lock.json ./apps/api/
RUN --mount=type=cache,id=sg-nuget,target=/root/.nuget/packages --mount=from=warm-nuget,target=/warm-packages --mount=type=secret,id=platform-ca if [ -d /warm-packages/npgsql.entityframeworkcore.postgresql ]; then mkdir -p /root/.nuget/packages; cp -a /warm-packages/. /root/.nuget/packages/; dotnet restore apps/api/SaigonGuide.Api.csproj --locked-mode --source /warm-packages; else if [ -f /run/secrets/platform-ca ]; then cat /etc/ssl/certs/ca-certificates.crt /run/secrets/platform-ca > /tmp/build-ca.pem; export SSL_CERT_FILE=/tmp/build-ca.pem; fi; dotnet restore apps/api/SaigonGuide.Api.csproj --locked-mode; fi
COPY apps/api/ ./apps/api/
RUN --mount=type=cache,id=sg-nuget,target=/root/.nuget/packages dotnet publish apps/api/SaigonGuide.Api.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
USER root
RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out/ ./
COPY --from=web /src/dist/ ./wwwroot/
COPY content/ /app/content/
RUN mkdir -p /data/media /data/keys && chown -R app:app /data /app
USER app
ENV ASPNETCORE_HTTP_PORTS=8080 Media__Path=/data/media Keys__Path=/data/keys Content__Path=/app/content
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s CMD curl --fail http://127.0.0.1:8080/health/ready || exit 1
ENTRYPOINT ["dotnet","SaigonGuide.Api.dll"]
