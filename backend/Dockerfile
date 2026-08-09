# =============================================================================
# Backend API — multi-stage build (.NET 8)
# Build context: thư mục backend/  →  docker build -f backend/Dockerfile backend
# =============================================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# global.json pin SDK 8.0.423 (rollForward latestMinor) — copy trước để restore ổn định.
COPY global.json ./
COPY src/ src/

RUN dotnet restore src/ClinicManagement.WebApi/ClinicManagement.WebApi.csproj
RUN dotnet publish src/ClinicManagement.WebApi/ClinicManagement.WebApi.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Chỉ phục vụ HTTP trong container; reverse proxy/hạ tầng lo TLS.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ClinicManagement.WebApi.dll"]
