# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so dependency layers are cached between builds
COPY src/Fundo.Applications.WebApi/Fundo.Applications.WebApi.csproj Fundo.Applications.WebApi/
RUN dotnet restore Fundo.Applications.WebApi/Fundo.Applications.WebApi.csproj

COPY src/Fundo.Applications.WebApi/ Fundo.Applications.WebApi/
RUN dotnet publish Fundo.Applications.WebApi/Fundo.Applications.WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run as the non-root user provided by the .NET 8 base image
USER $APP_UID

ENTRYPOINT ["dotnet", "Fundo.Applications.WebApi.dll"]
