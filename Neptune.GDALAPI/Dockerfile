#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# GDAL runs in-process through the MaxRev.Gdal NuGet packages (native libs ship in the publish output),
# so the base image is plain ASP.NET (was ghcr.io/osgeo/gdal:ubuntu-full-3.8.5 + a hand-installed
# dotnet runtime). GDAL stays in this container on purpose: it parses user-uploaded geodatabases in
# native code, and a native crash must not take Neptune.API down with it.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# Port 80 comes from ASPNETCORE_URLS in the helm configmap, as before.
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY ["Neptune.GDALAPI/Neptune.GDALAPI.csproj", "Neptune.GDALAPI/"]
RUN dotnet restore "Neptune.GDALAPI/Neptune.GDALAPI.csproj"
COPY . .
WORKDIR "/src/Neptune.GDALAPI"
RUN dotnet build "Neptune.GDALAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Neptune.GDALAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Neptune.GDALAPI.dll"]