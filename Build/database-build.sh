#!/usr/bin/env bash
# Builds the DacPac from the SQL project and deploys it to the devcontainer
# database. On Linux, generates a temporary SDK-style sqlproj (Microsoft.Build.Sql)
# since the original SSDT-style sqlproj requires Visual Studio targets.
#
# NOTE: authored for the Linux devcontainer, mirroring wave-runup's proven
# SDK-conversion approach, but NOT yet smoke-tested in a Neptune container. On
# Windows, Build/DatabaseBuild.ps1 remains the canonical path.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORKSPACE="$(cd "$SCRIPT_DIR/.." && pwd)"

DB_SERVER="${DB_SERVER:-neptune.db}"
DB_NAME="${DB_NAME:-NeptuneDB}"
DB_PASSWORD="${DB_PASSWORD:-DevPassword#1}"

DB_DIR="$WORKSPACE/Neptune.Database"
if [ "$(uname)" = "Linux" ]; then
    DACPAC="$DB_DIR/bin/Debug/Neptune.Database.linux.dacpac"
else
    DACPAC="$DB_DIR/bin/Debug/Neptune.Database.dacpac"
fi
PUBLISH_PROFILE="$DB_DIR/Neptune.Database.publish.xml"
ORIGINAL_SQLPROJ="$DB_DIR/Neptune.Database.sqlproj"
SDK_SQLPROJ="$DB_DIR/Neptune.Database.linux.sqlproj"

# Build a temporary SDK-style sqlproj from the original SSDT one. Extracts all
# <ItemGroup> blocks and wraps them in an SDK-style project (DSP Sql130 matches
# the original Neptune.Database.sqlproj).
echo "==> Generating SDK-style sqlproj for Linux build..."
ITEM_GROUPS=$(sed -n '/<ItemGroup>/,/<\/ItemGroup>/p' "$ORIGINAL_SQLPROJ")

cat > "$SDK_SQLPROJ" <<SQLPROJ
<Project DefaultTargets="Build">
  <Sdk Name="Microsoft.Build.Sql" Version="2.1.0" />
  <PropertyGroup>
    <Name>Neptune.Database</Name>
    <DSP>Microsoft.Data.Tools.Schema.Sql.Sql130DatabaseSchemaProvider</DSP>
    <ModelCollation>1033,CI</ModelCollation>
    <DefaultCollation>SQL_Latin1_General_CP1_CI_AS</DefaultCollation>
    <TargetDatabaseSet>True</TargetDatabaseSet>
    <EnableDefaultSqlItems>False</EnableDefaultSqlItems>
    <ProjectGuid>{18896e6a-80c1-423a-b354-62466f4de70d}</ProjectGuid>
  </PropertyGroup>
$ITEM_GROUPS
</Project>
SQLPROJ

echo "==> Building DacPac..."
dotnet build "$SDK_SQLPROJ" -c Debug

# Clean up the temporary sqlproj AND its NuGet restore artifacts in obj/. The
# SDK-style restore writes project.assets.json/nuget.g.* files that break the
# SSDT sqlproj build in Visual Studio.
rm -f "$SDK_SQLPROJ"
rm -rf "$DB_DIR/obj"

if [ ! -f "$DACPAC" ]; then
    echo "ERROR: DacPac not found at $DACPAC"
    exit 1
fi

echo "==> Deploying DacPac to $DB_SERVER/$DB_NAME..."
sqlpackage \
    /Action:Publish \
    /SourceFile:"$DACPAC" \
    /TargetServerName:"$DB_SERVER" \
    /TargetDatabaseName:"$DB_NAME" \
    /TargetUser:sa \
    /TargetPassword:"$DB_PASSWORD" \
    /TargetTrustServerCertificate:True \
    /Profile:"$PUBLISH_PROFILE"

echo "==> Database build and deploy complete."
