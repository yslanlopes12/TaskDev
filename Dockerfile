FROM node:24-alpine AS frontend
WORKDIR /src
COPY TaskDev-Front/package.json TaskDev-Front/package-lock.json ./
RUN npm ci
COPY TaskDev-Front/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY global.json ./
COPY TaskDev-Back/TaskDev-Back/TaskDev-Back.csproj ./
RUN dotnet restore TaskDev-Back.csproj
COPY TaskDev-Back/TaskDev-Back/ ./
RUN dotnet publish TaskDev-Back.csproj --no-restore -c Release -o /publish \
    -p:UseAppHost=false -p:DebugType=None

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS runtime
WORKDIR /app
COPY --from=backend /publish/ ./
COPY --from=frontend /src/dist/ ./wwwroot/
ENV ASPNETCORE_HTTP_PORTS=8080 \
    Frontend__ServeSpa=true \
    DataProtection__PersistKeysToPostgres=true \
    Security__SecureCookies=true \
    Swagger__Enabled=false \
    DOTNET_gcServer=0
EXPOSE 8080
USER ${APP_UID}
ENTRYPOINT ["dotnet", "TaskDev-Back.dll"]
