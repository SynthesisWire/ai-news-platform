FROM mcr.microsoft.com/dotnet/sdk:10.0.402 AS build
WORKDIR /src
COPY global.json ./
COPY src/backend/ src/backend/
RUN dotnet restore src/backend/AiResearch.Api/AiResearch.Api.csproj
RUN dotnet publish src/backend/AiResearch.Api/AiResearch.Api.csproj --no-restore -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "AiResearch.Api.dll"]
