#!/usr/bin/env bash
# Scaffolds EF Core entities from the database, then runs the custom
# EFCorePOCOGenerator — the two-step flow of Build/Scaffold.ps1.
#   1. dotnet ef dbcontext scaffold  (tables minus lookup tables minus exclude list)
#   2. build + run EFCorePOCOGenerator (POCOs, extension methods, TS enums)
#
# NOTE: authored for the Linux devcontainer, mirroring Scaffold.ps1, but NOT yet
# smoke-tested in a Neptune container. In particular the POCO generator is
# invoked with the same --db-server-name/--db-name args as Scaffold.ps1, which
# assume integrated auth; against the devcontainer's sa-auth SQL Server the
# generator may need to accept sa credentials. On Windows, Build/Scaffold.ps1
# remains the canonical path. See Build/dev-tf/README.md.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORKSPACE="$(cd "$SCRIPT_DIR/.." && pwd)"

DB_SERVER="${DB_SERVER:-neptune.db}"
DB_NAME="${DB_NAME:-NeptuneDB}"
DB_PASSWORD="${DB_PASSWORD:-DevPassword#1}"

TABLES_DIR="$WORKSPACE/Neptune.Database/dbo/Tables"
VIEWS_DIR="$WORKSPACE/Neptune.Database/dbo/Views"
LOOKUP_DIR="$WORKSPACE/Neptune.Database/Scripts/LookupTables"

EF_PROJECT="Neptune.EFModels"
EF_CONTEXT="NeptuneDbContext"
EF_NAMESPACE="Neptune.EFModels.Entities"

# Kept in sync with Build/build.ini TableExcludeList.
TABLE_EXCLUDE_LIST="dbo.DatabaseMigration,dbo.__RefactorLog,dbo.geometry_columns,dbo.gt_pk_metadata,dbo.spatial_ref_sys,dbo.vGeoServerDelineation,dbo.vGeoServerJurisdiction,dbo.vGeoServerLandUseBlock,dbo.vGeoServerMaskLayer,dbo.vGeoServerOCTAPrioritization,dbo.vGeoServerOnlandVisualTrashAssessmentArea,dbo.vGeoServerParcel,dbo.vGeoServerRegionalSubbasin,dbo.vGeoServerTrashGeneratingUnit,dbo.vGeoServerTrashGeneratingUnitLoad,dbo.vGeoServerTrashGeneratingUnitLoadBased,dbo.vGeoServerTreatmentBMPPointLocation,dbo.vGeoServerTreatmentBMPDelineation,dbo.vGeoServerWatershed,dbo.vStormwaterJurisdictionOrganizationMapping"

CONNECTION_STRING="Server=$DB_SERVER;Database=$DB_NAME;User Id=sa;Password=$DB_PASSWORD;TrustServerCertificate=True;"

# All table + view names from the SQL project (filename without extension).
declare -a ALL_TABLES=()
for dir in "$TABLES_DIR" "$VIEWS_DIR"; do
    if [ -d "$dir" ]; then
        for f in "$dir"/*.sql; do
            [ -f "$f" ] || continue
            ALL_TABLES+=("$(basename "$f" .sql)")
        done
    fi
done

# Lookup table names (enums) from the LookupTables directory.
declare -A LOOKUP_TABLES=()
LOOKUP_NAMES=()
if [ -d "$LOOKUP_DIR" ]; then
    for f in "$LOOKUP_DIR"/*.sql; do
        [ -f "$f" ] || continue
        name="$(basename "$f" .sql)"
        [[ "$name" == Script.PostDeployment.* ]] && continue
        LOOKUP_TABLES["$name"]=1
        LOOKUP_NAMES+=("$name")
    done
fi

# Exclude set from the exclude list.
declare -A EXCLUDED=()
IFS=',' read -ra EXCL_ITEMS <<< "$TABLE_EXCLUDE_LIST"
for item in "${EXCL_ITEMS[@]}"; do
    EXCLUDED["$item"]=1
done

# Include tables that are NOT lookup tables and NOT in the exclude list.
SCAFFOLD_TABLES=()
for table in "${ALL_TABLES[@]}"; do
    if [[ -z "${LOOKUP_TABLES[$table]:-}" ]] && [[ -z "${EXCLUDED[$table]:-}" ]]; then
        SCAFFOLD_TABLES+=("$table")
    fi
done

if [ ${#SCAFFOLD_TABLES[@]} -eq 0 ]; then
    echo "ERROR: No tables found to scaffold (is the schema deployed? run 'make build')."
    exit 1
fi

echo "==> Scaffolding ${#SCAFFOLD_TABLES[@]} tables..."
TABLE_ARGS=()
for table in "${SCAFFOLD_TABLES[@]}"; do
    TABLE_ARGS+=("--table" "$table")
done

cd "$WORKSPACE"
dotnet ef dbcontext scaffold \
    "$CONNECTION_STRING" \
    Microsoft.EntityFrameworkCore.SqlServer \
    --output-dir Entities/Generated \
    --project "$EF_PROJECT" \
    --context "$EF_CONTEXT" \
    --force \
    --startup-project "$EF_PROJECT" \
    --data-annotations \
    --use-database-names \
    --no-onconfiguring \
    --namespace "$EF_NAMESPACE" \
    "${TABLE_ARGS[@]}"

echo "==> Building EFCorePOCOGenerator..."
POCO_CSPROJ="$SCRIPT_DIR/efcorepocogenerator/EFCorePOCOGenerator/EFCorePOCOGenerator.csproj"
dotnet build "$POCO_CSPROJ" -c Debug

echo "==> Generating POCOs / extension methods / TS enums..."
POCO_DLL="$SCRIPT_DIR/efcorepocogenerator/EFCorePOCOGenerator/bin/Debug/net8.0/EFCorePOCOGenerator.dll"
ENUM_LIST="$(IFS=,; echo "${LOOKUP_NAMES[*]}")"
# Paths are relative to the Build directory, matching Build/build.ini.
cd "$SCRIPT_DIR"
dotnet "$POCO_DLL" \
    --db-server-name="$DB_SERVER" \
    --db-name="$DB_NAME" \
    --generate-primary-key-objects=true \
    --generate-enums-as-select-dropdown-options=true \
    --code-namespace="$EF_NAMESPACE" \
    --api-efmodels-output-dir="../Neptune.EFModels/Entities/Generated/ExtensionMethods" \
    --table-exclude-list="$TABLE_EXCLUDE_LIST" \
    --enum-list="$ENUM_LIST" \
    --typescript-enums-output-dir="../Neptune.Web/src/app/shared/generated/enum"

echo "==> Scaffold complete."
