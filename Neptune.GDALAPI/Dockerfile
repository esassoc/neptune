#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM ghcr.io/osgeo/gdal:ubuntu-full-3.8.5 as base
WORKDIR /app
EXPOSE 80

# Install ASP.NET Core 10.0 runtime via Microsoft's dotnet-install script.
# Avoids the dotnet/backports PPA (which requires reaching launchpad.net at build
# time and was timing out from the QA build agent).
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget ca-certificates && \
    rm -rf /var/lib/apt/lists/* && \
    wget -O /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh && \
    chmod +x /tmp/dotnet-install.sh && \
    /tmp/dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet && \
    rm /tmp/dotnet-install.sh && \
    ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet

ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="${PATH}:/usr/share/dotnet"

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