/* AUTO-GENERATED from swagger.json. DO NOT EDIT BY HAND. */

import { HttpInterceptorRouteConfig } from '@auth0/auth0-angular';

export type AllowedHttpMethod = 'GET'|'POST'|'PUT'|'PATCH'|'DELETE'|'OPTIONS'|'HEAD';

type ExactMap = Partial<Record<AllowedHttpMethod, ReadonlyArray<string>>>;
type RegexMap = Partial<Record<AllowedHttpMethod, ReadonlyArray<RegExp>>>;

function stripBase(apiBaseUrl: string, uri: string): string | null {
  const base = apiBaseUrl.endsWith('/') ? apiBaseUrl.slice(0, -1) : apiBaseUrl;

  // Only match our API base
  if (!uri.startsWith(base)) return null;

  // Remove the base. Ensure leading "/" for comparison with OpenAPI paths.
  const rest = uri.substring(base.length);
  if (rest === '') return '/';
  return rest.startsWith('/') ? rest : '/' + rest;
}

const ANON_EXACT: ExactMap = {
  'DELETE': [],
  'GET': ["/","/custom-attribute-types","/field-definitions","/jurisdictions/bounding-box","/jurisdictions/user-viewable","/land-use-blocks","/observation-types","/support-requests/site-key","/training-videos","/trash-generating-units/last-update-date","/treatment-bmp-type-custom-attribute-types","/treatment-bmp-types","/treatment-bmp-types/cards","/treatment-bmps","/treatment-bmps/modeling-attributes","/treatment-bmps/verified/feature-collection","/water-quality-management-plans/grid"],
  'POST': ["/support-requests"],
  'PUT': [],
};

const SECURED_EXACT: ExactMap = {
  'DELETE': ["/delineations/gdb/staging"],
  'GET': ["/delineations","/delineations/gdb/staging-report","/field-visits","/funding-sources","/hru-characteristics","/jurisdictions","/jurisdictions/manageable","/load-generating-units","/maintenance-records","/nereid/config","/nereid/delta-solve","/nereid/delta-solve-test","/nereid/health","/nereid/land-surface-loading","/nereid/land-surface-loading-baseline","/nereid/land-surface-table","/nereid/no-treatment-facility-validate","/nereid/solution-sequence","/nereid/solution-test-case","/nereid/subgraph","/nereid/total-network-graph","/nereid/treatment-facilities","/nereid/treatment-facility-validate","/nereid/treatment-sites","/nereid/validate","/onland-visual-trash-assessment-areas","/onland-visual-trash-assessments","/organization-types","/organizations","/parcels","/parcels/search","/projects","/projects/OCTAM2Tier2GrantProgram","/projects/OCTAM2Tier2GrantProgram/download","/projects/OCTAM2Tier2GrantProgram/treatmentBMPs/download","/projects/delineations","/projects/download","/projects/treatmentBMPs/download","/regional-subbasin-revision-requests","/regional-subbasins","/trash-generating-units","/treatment-bmp-assessments","/treatment-bmp-types/grid","/treatment-bmps/for-delineation-map","/treatment-bmps/octa-m2-tier2-grant-program","/treatment-bmps/planned-projects","/users","/water-quality-management-plan-documents","/water-quality-management-plan-verifies","/water-quality-management-plans","/water-quality-management-plans/display-dtos","/water-quality-management-plans/hydrologic-subareas","/water-quality-management-plans/source-control-bmp-attributes","/water-quality-management-plans/with-final-document"],
  'POST': ["/custom-attribute-types","/delineations/gdb/approve","/delineations/gdb/download","/delineations/gdb/upload","/funding-sources","/graph-trace-as-feature-collection-from-point","/hru-characteristics/enqueue-refresh","/observation-types","/onland-visual-trash-assessments","/organizations","/parcels/lookup-by-numbers","/projects","/regional-subbasins/enqueue-refresh","/treatment-bmp-types","/treatment-bmps","/user-claims","/users","/water-quality-management-plan-documents","/water-quality-management-plans","/water-quality-management-plans/upload"],
  'PUT': ["/land-use-blocks"],
};

const ANON_REGEX: RegexMap = {
  'DELETE': [

  ],
  'GET': [
    new RegExp("^/custom-attribute-types/[^/]+$"),
    new RegExp("^/custom-rich-texts/[^/]+$"),
    new RegExp("^/field-definitions/[^/]+$"),
    new RegExp("^/file-resources/[^/]+$"),
    new RegExp("^/jurisdictions/[^/]+/bounding-box$"),
    new RegExp("^/observation-types/[^/]+$"),
    new RegExp("^/onland-visual-trash-assessment-areas/jurisdictions/[^/]+$"),
    new RegExp("^/purpose/[^/]+$"),
    new RegExp("^/trash-generating-units/[^/]+$"),
    new RegExp("^/trash-results-by-jurisdiction/[^/]+/area-based-results-calculations$"),
    new RegExp("^/trash-results-by-jurisdiction/[^/]+/load-based-results-calculations$"),
    new RegExp("^/trash-results-by-jurisdiction/[^/]+/ovta-based-results-calculations$"),
    new RegExp("^/treatment-bmp-type-custom-attribute-types/[^/]+$"),
    new RegExp("^/treatment-bmp-type-custom-attribute-types/purpose/[^/]+$"),
    new RegExp("^/treatment-bmp-types/[^/]+/custom-attribute-types$"),
    new RegExp("^/treatment-bmp-types/[^/]+/detail$"),
    new RegExp("^/treatment-bmps/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/benchmarks-and-thresholds$"),
    new RegExp("^/treatment-bmps/[^/]+/benchmarks-and-thresholds/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/custom-attributes$"),
    new RegExp("^/treatment-bmps/[^/]+/field-visits$"),
    new RegExp("^/treatment-bmps/[^/]+/funding-events$"),
    new RegExp("^/treatment-bmps/[^/]+/funding-events/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/treatment-bmp-documents$"),
    new RegExp("^/treatment-bmps/[^/]+/treatment-bmp-documents/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/treatment-bmp-images$"),
    new RegExp("^/treatment-bmps/[^/]+/treatment-bmp-images/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/treatment-bmp-images/carousel$"),
    new RegExp("^/treatment-bmps/jurisdictions/[^/]+/verified/feature-collection$"),
    new RegExp("^/water-quality-management-plans/[^/]+$"),
    new RegExp("^/water-quality-management-plans/[^/]+/documents$"),
    new RegExp("^/water-quality-management-plans/[^/]+/hru-characteristics$"),
    new RegExp("^/water-quality-management-plans/[^/]+/modeled-performance$"),
    new RegExp("^/water-quality-management-plans/[^/]+/quick-bmps$"),
    new RegExp("^/water-quality-management-plans/[^/]+/source-control-bmps$"),
    new RegExp("^/water-quality-management-plans/[^/]+/verifications$"),
  ],
  'POST': [

  ],
  'PUT': [
    new RegExp("^/treatment-bmps/[^/]+/basic-info$"),
  ],
};

const SECURED_REGEX: RegexMap = {
  'DELETE': [
    new RegExp("^/custom-attribute-types/[^/]+$"),
    new RegExp("^/delineations/for-treatment-bmp/[^/]+$"),
    new RegExp("^/field-visits/[^/]+$"),
    new RegExp("^/funding-sources/[^/]+$"),
    new RegExp("^/maintenance-records/[^/]+$"),
    new RegExp("^/observation-types/[^/]+$"),
    new RegExp("^/onland-visual-trash-assessment-areas/[^/]+$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/observations/observation-photos/[^/]+$"),
    new RegExp("^/organizations/[^/]+$"),
    new RegExp("^/project-documents/[^/]+$"),
    new RegExp("^/projects/[^/]+$"),
    new RegExp("^/treatment-bmp-assessments/[^/]+$"),
    new RegExp("^/treatment-bmp-assessments/[^/]+/photos/[^/]+$"),
    new RegExp("^/treatment-bmp-types/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/benchmarks-and-thresholds/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/funding-events/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/treatment-bmp-documents/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/treatment-bmp-images/[^/]+$"),
    new RegExp("^/users/[^/]+$"),
    new RegExp("^/water-quality-management-plan-documents/[^/]+$"),
    new RegExp("^/water-quality-management-plans/[^/]+$"),
    new RegExp("^/water-quality-management-plans/[^/]+/verifications/[^/]+$"),
    new RegExp("^/water-quality-management-plans/[^/]+/verifications/[^/]+/supporting-documentation$"),
  ],
  'GET': [
    new RegExp("^/delineations/for-treatment-bmp/[^/]+$"),
    new RegExp("^/field-visits/[^/]+$"),
    new RegExp("^/field-visits/[^/]+/assessments/[^/]+$"),
    new RegExp("^/field-visits/[^/]+/maintenance-record$"),
    new RegExp("^/funding-sources/[^/]+$"),
    new RegExp("^/jurisdictions/[^/]+$"),
    new RegExp("^/jurisdictions/[^/]+/treatment-bmps$"),
    new RegExp("^/jurisdictions/[^/]+/users$"),
    new RegExp("^/load-generating-units/[^/]+$"),
    new RegExp("^/load-generating-units/[^/]+/hru-characteristics$"),
    new RegExp("^/maintenance-records/[^/]+$"),
    new RegExp("^/nereid/treatment-facility-validate/[^/]+$"),
    new RegExp("^/onland-visual-trash-assessment-areas/[^/]+$"),
    new RegExp("^/onland-visual-trash-assessment-areas/[^/]+/area-as-feature-collection$"),
    new RegExp("^/onland-visual-trash-assessment-areas/[^/]+/onland-visual-trash-assessments$"),
    new RegExp("^/onland-visual-trash-assessment-areas/[^/]+/parcel-geometries$"),
    new RegExp("^/onland-visual-trash-assessment-areas/[^/]+/transect-line-as-feature-collection$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/area-as-feature-collection$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/observations$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/observations/feature-collection$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/parcels$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/progress$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/review-and-finalize$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/select-area-context$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/transect-line-as-feature-collection$"),
    new RegExp("^/organizations/[^/]+$"),
    new RegExp("^/project-documents/[^/]+$"),
    new RegExp("^/projects/[^/]+$"),
    new RegExp("^/projects/[^/]+/attachments$"),
    new RegExp("^/projects/[^/]+/bounding-box$"),
    new RegExp("^/projects/[^/]+/delineations$"),
    new RegExp("^/projects/[^/]+/load-reducing-results$"),
    new RegExp("^/projects/[^/]+/progress$"),
    new RegExp("^/projects/[^/]+/project-network-solve-histories$"),
    new RegExp("^/projects/[^/]+/treatment-bmp-hru-characteristics$"),
    new RegExp("^/projects/[^/]+/treatment-bmps$"),
    new RegExp("^/projects/[^/]+/treatment-bmps/as-upsert-dtos$"),
    new RegExp("^/regional-subbasin-revision-requests/[^/]+$"),
    new RegExp("^/regional-subbasin-revision-requests/[^/]+/gdb$"),
    new RegExp("^/regional-subbasins/[^/]+$"),
    new RegExp("^/regional-subbasins/[^/]+/hru-characteristics$"),
    new RegExp("^/regional-subbasins/[^/]+/load-generating-units$"),
    new RegExp("^/regional-subbasins/upstream-delineation-for-bmp/[^/]+$"),
    new RegExp("^/treatment-bmp-assessments/[^/]+$"),
    new RegExp("^/treatment-bmp-assessments/[^/]+/photos$"),
    new RegExp("^/treatment-bmps/[^/]+/delineation-errors$"),
    new RegExp("^/treatment-bmps/[^/]+/field-visits/in-progress$"),
    new RegExp("^/treatment-bmps/[^/]+/hru-characteristics$"),
    new RegExp("^/treatment-bmps/[^/]+/other-treatment-bmps-in-regional-subbasin$"),
    new RegExp("^/treatment-bmps/[^/]+/parameterization-errors$"),
    new RegExp("^/treatment-bmps/[^/]+/upstreamRSBCatchmentGeoJSON$"),
    new RegExp("^/treatment-bmps/[^/]+/upstreamest-errors$"),
    new RegExp("^/user-claims/[^/]+$"),
    new RegExp("^/users/[^/]+$"),
    new RegExp("^/users/[^/]+/detail$"),
    new RegExp("^/users/[^/]+/notifications$"),
    new RegExp("^/water-quality-management-plan-documents/[^/]+$"),
    new RegExp("^/water-quality-management-plans/[^/]+/available-treatment-bmps$"),
    new RegExp("^/water-quality-management-plans/[^/]+/boundary$"),
    new RegExp("^/water-quality-management-plans/[^/]+/extraction-result$"),
    new RegExp("^/water-quality-management-plans/[^/]+/parcel-ids$"),
    new RegExp("^/water-quality-management-plans/[^/]+/verifications/[^/]+$"),
  ],
  'POST': [
    new RegExp("^/ai/water-quality-management-plan-documents/[^/]+/ask$"),
    new RegExp("^/ai/water-quality-management-plan-documents/[^/]+/extract-all$"),
    new RegExp("^/delineations/[^/]+/set-verified$"),
    new RegExp("^/field-visits/[^/]+/assessments/[^/]+$"),
    new RegExp("^/field-visits/[^/]+/finalize$"),
    new RegExp("^/field-visits/[^/]+/maintenance-record$"),
    new RegExp("^/field-visits/[^/]+/mark-provisional$"),
    new RegExp("^/field-visits/[^/]+/return-to-edit$"),
    new RegExp("^/field-visits/[^/]+/verify$"),
    new RegExp("^/onland-visual-trash-assessment-areas/[^/]+/move-assessments$"),
    new RegExp("^/onland-visual-trash-assessment-areas/[^/]+/parcel-geometries$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/observations$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/observations/observation-photo$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/parcels$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/refine-area$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/refresh-parcels$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/return-to-edit$"),
    new RegExp("^/onland-visual-trash-assessments/[^/]+/review-and-finalize$"),
    new RegExp("^/projects/[^/]+/attachments$"),
    new RegExp("^/projects/[^/]+/copy$"),
    new RegExp("^/projects/[^/]+/modeled-performance$"),
    new RegExp("^/regional-subbasin-revision-requests/[^/]+/close$"),
    new RegExp("^/regional-subbasin-revision-requests/for-treatment-bmp/[^/]+$"),
    new RegExp("^/treatment-bmp-assessments/[^/]+/copy-from-initial$"),
    new RegExp("^/treatment-bmp-assessments/[^/]+/photos$"),
    new RegExp("^/treatment-bmps/[^/]+/benchmarks-and-thresholds$"),
    new RegExp("^/treatment-bmps/[^/]+/field-visits$"),
    new RegExp("^/treatment-bmps/[^/]+/funding-events$"),
    new RegExp("^/treatment-bmps/[^/]+/treatment-bmp-documents$"),
    new RegExp("^/treatment-bmps/[^/]+/treatment-bmp-images$"),
    new RegExp("^/water-quality-management-plans/[^/]+/extract$"),
    new RegExp("^/water-quality-management-plans/[^/]+/promote$"),
    new RegExp("^/water-quality-management-plans/[^/]+/save-basics$"),
    new RegExp("^/water-quality-management-plans/[^/]+/save-bmps$"),
    new RegExp("^/water-quality-management-plans/[^/]+/save-location$"),
    new RegExp("^/water-quality-management-plans/[^/]+/verifications$"),
    new RegExp("^/water-quality-management-plans/[^/]+/verifications/[^/]+/supporting-documentation$"),
  ],
  'PUT': [
    new RegExp("^/custom-attribute-types/[^/]+$"),
    new RegExp("^/custom-rich-texts/[^/]+$"),
    new RegExp("^/delineations/for-treatment-bmp/[^/]+$"),
    new RegExp("^/field-definitions/[^/]+$"),
    new RegExp("^/field-visits/[^/]+/date-and-type$"),
    new RegExp("^/field-visits/[^/]+/inventory-updated$"),
    new RegExp("^/funding-sources/[^/]+$"),
    new RegExp("^/maintenance-records/[^/]+$"),
    new RegExp("^/observation-types/[^/]+$"),
    new RegExp("^/onland-visual-trash-assessment-areas/[^/]+$"),
    new RegExp("^/organizations/[^/]+$"),
    new RegExp("^/project-documents/[^/]+$"),
    new RegExp("^/projects/[^/]+/delineations$"),
    new RegExp("^/projects/[^/]+/treatment-bmps$"),
    new RegExp("^/projects/[^/]+/treatment-bmps/update-locations$"),
    new RegExp("^/projects/[^/]+/update$"),
    new RegExp("^/treatment-bmp-assessments/[^/]+/observations$"),
    new RegExp("^/treatment-bmp-assessments/[^/]+/photos/[^/]+$"),
    new RegExp("^/treatment-bmp-types/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/benchmarks-and-thresholds/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/custom-attribute-type-purposes/[^/]+/custom-attributes$"),
    new RegExp("^/treatment-bmps/[^/]+/funding-events/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/location$"),
    new RegExp("^/treatment-bmps/[^/]+/queue-refresh-land-use$"),
    new RegExp("^/treatment-bmps/[^/]+/treatment-bmp-documents/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/treatment-bmp-images$"),
    new RegExp("^/treatment-bmps/[^/]+/treatment-bmp-types/[^/]+$"),
    new RegExp("^/treatment-bmps/[^/]+/type$"),
    new RegExp("^/treatment-bmps/[^/]+/upstream-bmp$"),
    new RegExp("^/users/[^/]+$"),
    new RegExp("^/users/[^/]+/active-status$"),
    new RegExp("^/users/[^/]+/jurisdictions$"),
    new RegExp("^/users/[^/]+/role$"),
    new RegExp("^/water-quality-management-plan-documents/[^/]+$"),
    new RegExp("^/water-quality-management-plans/[^/]+$"),
    new RegExp("^/water-quality-management-plans/[^/]+/boundary$"),
    new RegExp("^/water-quality-management-plans/[^/]+/modeling-approach$"),
    new RegExp("^/water-quality-management-plans/[^/]+/parcels$"),
    new RegExp("^/water-quality-management-plans/[^/]+/quick-bmps$"),
    new RegExp("^/water-quality-management-plans/[^/]+/source-control-bmps$"),
    new RegExp("^/water-quality-management-plans/[^/]+/treatment-bmps$"),
    new RegExp("^/water-quality-management-plans/[^/]+/verifications/[^/]+$"),
  ],
};

/**
 * Auth0 httpInterceptor.allowedList generator.
 *
 * This generates route configs that:
 * - For anonymous routes: attach token if user is logged in, allow request if not (allowAnonymous: true)
 * - For secured routes: require token (no allowAnonymous flag)
 *
 * This allows endpoints like /projects to work for both anonymous users (showing public data)
 * and authenticated users (showing additional data based on their identity).
 */
export function buildAuth0AllowedList(apiBaseUrl: string): HttpInterceptorRouteConfig[] {
  const methods: AllowedHttpMethod[] = ['GET','POST','PUT','PATCH','DELETE','OPTIONS','HEAD'];
  const entries: HttpInterceptorRouteConfig[] = [];
  const base = apiBaseUrl.endsWith('/') ? apiBaseUrl.slice(0, -1) : apiBaseUrl;

  // Anonymous routes: attach token if available, allow anonymous access
  for (const method of methods) {
    const exactPaths = ANON_EXACT[method];
    if (exactPaths) {
      for (const path of exactPaths) {
        entries.push({
          uri: base + path,
          httpMethod: method,
          allowAnonymous: true
        });
      }
    }
    const regexPatterns = ANON_REGEX[method];
    if (regexPatterns) {
      for (const rx of regexPatterns) {
        entries.push({
          uriMatcher: (uri: string) => {
            const p = stripBase(apiBaseUrl, uri);
            return p !== null && rx.test(p);
          },
          httpMethod: method,
          allowAnonymous: true
        });
      }
    }
  }

  // Secured routes: require token
  for (const method of methods) {
    const exactPaths = SECURED_EXACT[method];
    if (exactPaths) {
      for (const path of exactPaths) {
        entries.push({
          uri: base + path,
          httpMethod: method
        });
      }
    }
    const regexPatterns = SECURED_REGEX[method];
    if (regexPatterns) {
      for (const rx of regexPatterns) {
        entries.push({
          uriMatcher: (uri: string) => {
            const p = stripBase(apiBaseUrl, uri);
            return p !== null && rx.test(p);
          },
          httpMethod: method
        });
      }
    }
  }

  return entries;
}
