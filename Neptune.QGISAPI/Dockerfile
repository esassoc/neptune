#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM qgis/qgis:release-3_28 as base
# Set environment variables for QGIS
ENV QGIS_PREFIX_PATH=/usr
ENV QT_QPA_PLATFORM=offscreen
ENV XDG_RUNTIME_DIR=/tmp/runtime-root

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
COPY ["Neptune.QGISAPI/Neptune.QGISAPI.csproj", "Neptune.QGISAPI/"]
RUN dotnet restore "Neptune.QGISAPI/Neptune.QGISAPI.csproj"
COPY . .
WORKDIR "/src/Neptune.QGISAPI"
RUN dotnet build "Neptune.QGISAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Neptune.QGISAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# copy the python scripts
COPY --from=publish /src/Neptune.QGISAPI/pyqgis_utils.py .
COPY --from=publish /src/Neptune.QGISAPI/ComputeTrashGeneratingUnits.py .
COPY --from=publish /src/Neptune.QGISAPI/ModelingOverlayAnalysis.py .

ENTRYPOINT ["dotnet", "Neptune.QGISAPI.dll"]