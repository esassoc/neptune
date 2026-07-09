#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# NPT-1105: QGIS retired — the overlay runs in-process on NetTopologySuite, so the base image is
# plain ASP.NET (was qgis/qgis:release-3_28 + a hand-installed dotnet runtime, ~4-6 GB).
# Deliberately no `USER app`: the Helm configmap binds ASPNETCORE_URLS=http://+:80, and a non-root
# user cannot bind ports below 1024.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Neptune.OverlayAPI/Neptune.OverlayAPI.csproj", "Neptune.OverlayAPI/"]
RUN dotnet restore "Neptune.OverlayAPI/Neptune.OverlayAPI.csproj"
COPY . .
WORKDIR "/src/Neptune.OverlayAPI"
RUN dotnet build "Neptune.OverlayAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Neptune.OverlayAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Neptune.OverlayAPI.dll"]
