.PHONY: help auth start stop api web api-test web-test web-build web-lint database download restore build scaffold gen-models

WORKSPACE := /workspace
BUILD_DIR := $(WORKSPACE)/Build
API_PORT ?= 8250
WEB_PORT ?= 8252

help: ## Show available targets
	@grep -E '^[a-zA-Z_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-20s\033[0m %s\n", $$1, $$2}'

auth: ## One-time sign-in to Azure (az) + GitHub (gh); persists in shared volumes
	cd $(BUILD_DIR) && bash auth.sh

# --- App ---

start: stop ## Start API and web dev server in parallel (frees stale ports first)
	@trap 'kill 0' EXIT; \
	$(MAKE) api & \
	$(MAKE) web & \
	wait

stop: ## Kill stray API/web dev servers from a previous run (frees ports $(API_PORT)/$(WEB_PORT))
	@echo "Freeing dev ports (killing any stray watchers/servers from a previous run)..."
	-@pkill -9 -f "Neptune\.API" 2>/dev/null
	-@pkill -9 -f "ng serve" 2>/dev/null
	@sleep 1

api: ## Start API with hot-reload on port $(API_PORT) (Hangfire runs in-process)
	cd $(WORKSPACE) && dotnet watch --project Neptune.API -- run --urls http://+:$(API_PORT)

api-test: ## Run .NET tests
	cd $(WORKSPACE) && dotnet test Neptune.Tests/Neptune.Tests.csproj

# --- Web ---

web: ## Start Angular dev server on port $(WEB_PORT)
	cd $(WORKSPACE)/Neptune.Web && npx --yes ng serve --host 0.0.0.0 --port $(WEB_PORT) --poll 2000

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
