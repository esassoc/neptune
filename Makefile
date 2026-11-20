.PHONY: help auth start stop api gdalapi overlayapi web api-test web-test web-build web-lint database download restore build scaffold gen-models

WORKSPACE := /workspace
BUILD_DIR := $(WORKSPACE)/Build
API_PORT ?= 8250
WEB_PORT ?= 8252
# Container-internal only: Neptune.API reaches these on localhost (see GDALAPIBaseUrl /
# OverlayAPIBaseUrl in .devcontainer/.env), so they are not published to the host.
GDALAPI_PORT ?= 8231
OVERLAYAPI_PORT ?= 8232
# Point the API at the services this Makefile starts. Exported here, not only in .devcontainer/.env,
# so the URLs always follow the ports above and a container created before this change (env_file is
# read at container creation) works without a rebuild. Override on the command line to aim the API at
# a GDAL/Overlay API running elsewhere: make api GDALAPIBaseUrl=http://host.docker.internal:8231
export GDALAPIBaseUrl := http://localhost:$(GDALAPI_PORT)
export OverlayAPIBaseUrl := http://localhost:$(OVERLAYAPI_PORT)

# ESA laptops: Cato TLS inspection re-signs platform.claude.com, so the Claude CLI that local AI
# extraction runs (NPT-1133) must trust the Cato root. When the developer has exported it (see
# CLAUDE.md), hand it to everything make starts; elsewhere the file is absent and this is a no-op.
CORP_ROOT_CERT := $(WORKSPACE)/docker-compose/corp-root.crt
ifneq ($(wildcard $(CORP_ROOT_CERT)),)
export NODE_EXTRA_CA_CERTS ?= $(CORP_ROOT_CERT)
endif

help: ## Show available targets
	@grep -E '^[a-zA-Z_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-20s\033[0m %s\n", $$1, $$2}'

auth: ## One-time sign-in to Azure (az) + GitHub (gh); persists in shared volumes
	cd $(BUILD_DIR) && bash auth.sh

# --- App ---

# The GDAL and Overlay APIs are plain .NET services now (GDAL runs in-process via MaxRev, NPT-1154;
# the overlay runs on NetTopologySuite, NPT-1105), so they run here beside the API instead of needing
# the docker-compose/ stack on the host. They are built once up front, one after the other, and then
# started with --no-build: three parallel builds would race on the shared Neptune.Common obj folder.
# A build failure in one of them is reported but does not stop the API and web from starting.
start: stop ## Start API, GDAL API, Overlay API and web dev server (frees stale ports first)
	-@cd $(WORKSPACE) && dotnet build Neptune.GDALAPI/Neptune.GDALAPI.csproj -v q --nologo
	-@cd $(WORKSPACE) && dotnet build Neptune.OverlayAPI/Neptune.OverlayAPI.csproj -v q --nologo
	@trap 'kill 0' EXIT; \
	$(MAKE) gdalapi SERVICE_RUN_FLAGS=--no-build & \
	$(MAKE) overlayapi SERVICE_RUN_FLAGS=--no-build & \
	$(MAKE) api & \
	$(MAKE) web & \
	wait

stop: ## Kill stray dev servers from a previous run (frees ports $(API_PORT)/$(WEB_PORT)/$(GDALAPI_PORT)/$(OVERLAYAPI_PORT))
	@echo "Freeing dev ports (killing any stray watchers/servers from a previous run)..."
	-@pkill -9 -f "Neptune\.API" 2>/dev/null
	-@pkill -9 -f "Neptune\.GDALAPI" 2>/dev/null
	-@pkill -9 -f "Neptune\.OverlayAPI" 2>/dev/null
	-@pkill -9 -f "ng serve" 2>/dev/null
	@sleep 1

# --urls is required: the container sets ASPNETCORE_URLS to the API's port for every process.
gdalapi: ## Start the GDAL API (GDB import/export conversions) on port $(GDALAPI_PORT)
	cd $(WORKSPACE) && dotnet run --project Neptune.GDALAPI --no-launch-profile $(SERVICE_RUN_FLAGS) -- --urls http://+:$(GDALAPI_PORT)

overlayapi: ## Start the Overlay API (LGU/TGU overlays) on port $(OVERLAYAPI_PORT)
	cd $(WORKSPACE) && dotnet run --project Neptune.OverlayAPI --no-launch-profile $(SERVICE_RUN_FLAGS) -- --urls http://+:$(OVERLAYAPI_PORT)

api: ## Start API with hot-reload on port $(API_PORT) (Hangfire runs in-process)
	cd $(WORKSPACE) && dotnet watch --project Neptune.API -- run --urls http://+:$(API_PORT)

api-test: ## Run .NET tests
	cd $(WORKSPACE) && dotnet test Neptune.Tests/Neptune.Tests.csproj

# --- Web ---

# HTTPS with the tracked *.localhost.sitkatech.com certificate (Neptune.Web/server.crt), the one
# Windows `npm start` uses. Browse https://neptune.localhost.sitkatech.com:$(WEB_PORT) — Auth0 returns
# to that origin, which must be registered in the Auth0 app (8252 is). The devcontainer configuration
# points the SPA at this container's API (API_PORT).
# --allowed-hosts because the browser sends the neptune.localhost.sitkatech.com Host header.
web: ## Start Angular dev server on https://neptune.localhost.sitkatech.com:$(WEB_PORT)
	cd $(WORKSPACE)/Neptune.Web && npx --yes ng serve --configuration devcontainer --host 0.0.0.0 --port $(WEB_PORT) \
		--ssl --ssl-cert server.crt --ssl-key server.key --allowed-hosts --poll 2000

web-test: ## Run Angular unit tests
	cd $(WORKSPACE)/Neptune.Web && npm test

web-build: ## Build Angular for production
	cd $(WORKSPACE)/Neptune.Web && npm run build-prod

web-lint: ## Lint Angular code
	cd $(WORKSPACE)/Neptune.Web && npm run lint

# --- Database (chain: make download restore build scaffold) ---

database: ## Database context (chain with: download restore build scaffold)
	@echo ""
	@echo "  Database targets — chain any combination after 'make database':"
	@echo ""
	@echo "    \033[36mdownload\033[0m   Download BACPAC from Azure (requires az login)"
	@echo "    \033[36mrestore\033[0m    Restore database from BACPAC"
	@echo "    \033[36mbuild\033[0m      Build DacPac and deploy schema"
	@echo "    \033[36mscaffold\033[0m   Scaffold EF Core entities + run the POCO generator"
	@echo ""
	@echo "  Examples:"
	@echo "    make download restore build scaffold"
	@echo "    make build scaffold"
	@echo ""

download: ## Download BACPAC from Azure
	cd $(BUILD_DIR) && bash database-download.sh

restore: ## Restore database from BACPAC
	cd $(BUILD_DIR) && bash database-restore.sh

build: ## Build DacPac and deploy schema
	cd $(BUILD_DIR) && bash database-build.sh

scaffold: ## Scaffold EF Core entities + run the POCO generator
	cd $(BUILD_DIR) && bash scaffold.sh

# --- Code generation ---

gen-models: ## Regenerate TypeScript models from swagger.json
	cd $(WORKSPACE)/Neptune.Web && npm run gen-model
