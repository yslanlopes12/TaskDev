FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TaskDev-Back.csproj .
RUN dotnet restore TaskDev-Back.csproj
COPY . .
RUN dotnet publish TaskDev-Back.csproj --no-restore -c Release -o /publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app /home/app/.aspnet
COPY --from=build /publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER ${APP_UID}
ENTRYPOINT ["dotnet", "TaskDev-Back.dll"]
