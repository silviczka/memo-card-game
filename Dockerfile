FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MemoCardGame.Api/MemoCardGame.Api.csproj", "src/MemoCardGame.Api/"]
COPY ["src/MemoCardGame.Client/MemoCardGame.Client.csproj", "src/MemoCardGame.Client/"]
COPY ["src/MemoCardGame.Application/MemoCardGame.Application.csproj", "src/MemoCardGame.Application/"]
COPY ["src/MemoCardGame.Infrastructure/MemoCardGame.Infrastructure.csproj", "src/MemoCardGame.Infrastructure/"]
COPY ["src/MemoCardGame.Domain/MemoCardGame.Domain.csproj", "src/MemoCardGame.Domain/"]
RUN dotnet restore "src/MemoCardGame.Api/MemoCardGame.Api.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "src/MemoCardGame.Api/MemoCardGame.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/MemoCardGame.Api/MemoCardGame.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MemoCardGame.Api.dll"]
