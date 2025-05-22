# BASE image cho runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# BUILD stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy từng csproj để tận dụng cache restore cho lần rebuild tiếp theo
COPY MovieTheater.API/MovieTheater.API.csproj MovieTheater.API/
COPY MovieTheater.Application/MovieTheater.Application.csproj MovieTheater.Application/
COPY MovieTheater.Infrastructure/MovieTheater.Infrastructure.csproj MovieTheater.Infrastructure/
COPY MovieTheater.Domain/MovieTheater.Domain.csproj MovieTheater.Domain/

RUN dotnet restore MovieTheater.API/MovieTheater.API.csproj

# Sau restore xong mới copy toàn bộ source code
COPY . .
WORKDIR /src/MovieTheater.API
RUN dotnet build MovieTheater.API.csproj -c $BUILD_CONFIGURATION -o /app/build

# PUBLISH stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish MovieTheater.API.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# FINAL image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MovieTheater.API.dll"]
