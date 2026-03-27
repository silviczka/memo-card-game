# Docker image for MemoCardGame.Api (ASP.NET Core 8).
# Use this when deploying only the API host (e.g. Koyeb, Fly.io, Railway).
#
# The published output is MemoCardGame.Api.dll (same base name as MemoCardGame.Api.csproj).
# This project also references MemoCardGame.Client, so `dotnet publish` bundles the
# Blazor WebAssembly static files into the same image when you want API + UI on one host.

# Runtime image (no SDK) — smaller final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Koyeb and many PaaS expect HTTP on one port (often 8080)
EXPOSE 8080

# Build stage — restore, build, publish the API project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Layer-friendly restore: copy only csproj files first
COPY ["src/MemoCardGame.Api/MemoCardGame.Api.csproj", "src/MemoCardGame.Api/"]
COPY ["src/MemoCardGame.Client/MemoCardGame.Client.csproj", "src/MemoCardGame.Client/"]
COPY ["src/MemoCardGame.Application/MemoCardGame.Application.csproj", "src/MemoCardGame.Application/"]
COPY ["src/MemoCardGame.Infrastructure/MemoCardGame.Infrastructure.csproj", "src/MemoCardGame.Infrastructure/"]
COPY ["src/MemoCardGame.Domain/MemoCardGame.Domain.csproj", "src/MemoCardGame.Domain/"]
RUN dotnet restore "src/MemoCardGame.Api/MemoCardGame.Api.csproj"
COPY . .
RUN dotnet publish "src/MemoCardGame.Api/MemoCardGame.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Bind to all interfaces so the container accepts traffic from the platform proxy
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "MemoCardGame.Api.dll"]
