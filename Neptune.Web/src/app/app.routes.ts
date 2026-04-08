import { Routes } from "@angular/router";
import { ManagerOnlyGuard } from "./shared/guards/unauthenticated-access/manager-only-guard";
import { JurisdictionManagerOrEditorOnlyGuard } from "./shared/guards/unauthenticated-access/jurisdiction-manager-or-editor-only-guard.guard";
import { UnsavedChangesGuard } from "./shared/guards/unsaved-changes.guard";
import { OCTAGrantReviewerOnlyGuard } from "./shared/guards/unauthenticated-access/octa-grant-reviewer-only.guard";
import { authGuardFn } from "@auth0/auth0-angular";
import { AuthCallbackComponent } from "./auth-callback.component";

export const routeParams = {
    definitionID: "definitionID",
    projectID: "projectID",
    onlandVisualTrashAssessmentID: "onlandVisualTrashAssessmentID",
    onlandVisualTrashAssessmentAreaID: "onlandVisualTrashAssessmentAreaID",
    treatmentBMPID: "treatmentBMPID",
    loadGeneratingUnitID: "loadGeneratingUnitID",
    jurisdictionID: "jurisdictionID",
    regionalSubbasinID: "regionalSubbasinID",
    customAttributePurposeID: "customAttributePurposeID",
    waterQualityManagementPlanID: "waterQualityManagementPlanID",
};

export const routes: Routes = [
    {
        path: "ai",
        title: "AI Module",
        loadComponent: () => import("./pages/ai-module/ai-site-layout.component").then((m) => m.AiSiteLayoutComponent),
        children: [
            {
                path: "",
                title: "AI Home",
                loadComponent: () => import("./pages/ai-module/ai-home/ai-home.component").then((m) => m.AiHomeComponent),
            },
        ],
    },
    {
        path: `planning`,
        title: "Stormwater Tools",
        loadComponent: () => import("./pages/planning-module/planning-site-layout/planning-site-layout.component").then((m) => m.PlanningSiteLayoutComponent),
        children: [
            {
                path: "",
                title: "Home",
                loadComponent: () => import("./pages/planning-module/planning-home/planning-home/planning-home.component").then((m) => m.PLanningHomeComponent),
            },
            {
                path: "about",
                loadComponent: () => import("./pages/planning-module/planning-about/planning-about.component").then((m) => m.PlanningAboutComponent),
                canActivate: [authGuardFn],
            },
            {
                path: "grant-programs",
                title: "Grant Program",
                canActivate: [authGuardFn, OCTAGrantReviewerOnlyGuard],
                children: [
                    {
                        path: "octa-m2-tier-2",
                        loadComponent: () =>
                            import("./pages/planning-module/grant-programs/octa-m2-tier2-dashboard/octa-m2-tier2-dashboard.component").then((m) => m.OCTAM2Tier2DashboardComponent),
                    },
                ],
            },
            {
                path: "projects",
                title: "Projects",
                canActivate: [authGuardFn],
                children: [
                    {
                        path: "",
                        loadComponent: () => import("./pages/planning-module/projects/project-list/project-list.component").then((m) => m.ProjectListComponent),
                        canActivate: [JurisdictionManagerOrEditorOnlyGuard],
                    },
                    {
                        path: "new",
                        loadComponent: () => import("./pages/planning-module/project-workflow/project-workflow-outlet.component").then((m) => m.ProjectWorkflowOutletComponent),
                        canActivate: [JurisdictionManagerOrEditorOnlyGuard],
                        children: [
                            { path: "", redirectTo: "instructions", pathMatch: "full" },
                            {
                                path: "instructions",
                                title: "Instructions",
                                loadComponent: () =>
                                    import("./pages/planning-module/project-workflow/project-instructions/project-instructions.component").then(
                                        (m) => m.ProjectInstructionsComponent
                                    ),
                            },
                            {
                                path: "project-basics",
                                title: "Basics",
                                loadComponent: () =>
                                    import("./pages/planning-module/project-workflow/project-basics/project-basics.component").then((m) => m.ProjectBasicsComponent),
                                canDeactivate: [UnsavedChangesGuard],
                            },
                        ],
                    },
                    {
                        path: `edit/:${routeParams.projectID}`,
                        loadComponent: () => import("./pages/planning-module/project-workflow/project-workflow-outlet.component").then((m) => m.ProjectWorkflowOutletComponent),
                        canActivate: [JurisdictionManagerOrEditorOnlyGuard],
                        children: [
                            { path: "", redirectTo: "instructions", pathMatch: "full" },
                            {
                                path: "instructions",
                                title: "Instructions",
                                loadComponent: () =>
                                    import("./pages/planning-module/project-workflow/project-instructions/project-instructions.component").then(
                                        (m) => m.ProjectInstructionsComponent
                                    ),
                            },
                            {
                                path: "project-basics",
                                title: "Basics",
                                loadComponent: () =>
                                    import("./pages/planning-module/project-workflow/project-basics/project-basics.component").then((m) => m.ProjectBasicsComponent),
                                canDeactivate: [UnsavedChangesGuard],
                            },
                            {
                                path: "stormwater-treatments",
                                children: [
                                    { path: "", redirectTo: "treatment-bmps", pathMatch: "full" },
                                    {
                                        path: "treatment-bmps",
                                        title: "Treatment BMPs",
                                        loadComponent: () =>
                                            import("./pages/planning-module/project-workflow/treatment-bmps/treatment-bmps.component").then((m) => m.TreatmentBmpsComponent),
                                        canDeactivate: [UnsavedChangesGuard],
                                    },
                                    {
                                        path: "delineations",
                                        title: "Delineations",
                                        loadComponent: () =>
                                            import("./pages/planning-module/project-workflow/delineations/delineations.component").then((m) => m.DelineationsComponent),
                                        canDeactivate: [UnsavedChangesGuard],
                                    },
                                    {
                                        path: "modeled-performance-and-metrics",
                                        title: "Modeled Performance and Metrics",
                                        loadComponent: () =>
                                            import("./pages/planning-module/project-workflow/modeled-performance/modeled-performance.component").then(
                                                (m) => m.ModeledPerformanceComponent
                                            ),
                                    },
                                ],
                            },
                            {
                                path: "attachments",
                                title: "Attachments",
                                loadComponent: () =>
                                    import("./pages/planning-module/project-workflow/project-attachments/project-attachments.component").then((m) => m.ProjectAttachmentsComponent),
                            },
                            {
                                path: "review-and-share",
                                title: "Review and Share",
                                loadComponent: () => import("./pages/planning-module/project-workflow/review/review.component").then((m) => m.ReviewComponent),
                            },
                        ],
                    },
                    {
                        path: `:${routeParams.projectID}`,
                        loadComponent: () => import("./pages/planning-module/projects/project-detail/project-detail.component").then((m) => m.ProjectDetailComponent),
                    },
                ],
            },
            {
                path: "planning-map",
                title: "Planning Map",
                loadComponent: () => import("./pages/planning-module/planning-map/planning-map.component").then((m) => m.PlanningMapComponent),
                canActivate: [authGuardFn, JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "training",
                title: "Training",
                loadComponent: () => import("./pages/planning-module/training/training.component").then((m) => m.TrainingComponent),
                canActivate: [authGuardFn],
            },
        ],
    },
    {
        path: `trash`,
        title: "Stormwater Tools",
        loadComponent: () => import("./pages/trash-module/trash-site-layout/trash-site-layout.component").then((m) => m.TrashSiteLayoutComponent),
        children: [
            { path: "", title: "Home", loadComponent: () => import("./pages/trash-module/trash-home/trash-home.component").then((m) => m.TrashHomeComponent) },
            {
                path: "land-use-blocks",
                title: "Land Use Blocks",
                loadComponent: () => import("./pages/trash-module/trash-land-use-block-index/trash-land-use-block-index.component").then((m) => m.TrashLandUseBlockIndexComponent),
                canActivate: [authGuardFn],
            },
            {
                path: "onland-visual-trash-assessments",
                title: "OVTAs",
                children: [
                    {
                        path: "",
                        loadComponent: () => import("./pages/trash-module/ovtas/trash-ovta-index/trash-ovta-index.component").then((m) => m.TrashOvtaIndexComponent),
                        canActivate: [authGuardFn],
                    },
                    {
                        path: `:${routeParams.onlandVisualTrashAssessmentID}`,
                        loadComponent: () => import("./pages/trash-module/ovtas/trash-ovta-detail/trash-ovta-detail.component").then((m) => m.TrashOvtaDetailComponent),
                        canActivate: [authGuardFn],
                    },
                ],
            },
            {
                path: "onland-visual-trash-assessments/new",
                title: "OVTAs",
                loadComponent: () =>
                    import("./pages/trash-module/ovta-workflow/trash-ovta-workflow-outlet/trash-ovta-workflow-outlet.component").then((m) => m.TrashOvtaWorkflowOutletComponent),
                children: [
                    { path: "", redirectTo: "instructions", pathMatch: "full" },
                    {
                        path: "instructions",
                        title: "Instructions",
                        loadComponent: () =>
                            import("./pages/trash-module/ovta-workflow/trash-ovta-instructions/trash-ovta-instructions.component").then((m) => m.TrashOvtaInstructionsComponent),
                        canActivate: [authGuardFn],
                    },
                    {
                        path: "initiate-ovta",
                        title: "Initiate OVTA",
                        loadComponent: () =>
                            import("./pages/trash-module/ovta-workflow/trash-initiate-ovta/trash-initiate-ovta.component").then((m) => m.TrashInitiateOvtaComponent),
                        canActivate: [authGuardFn],
                    },
                ],
            },
            {
                path: `onland-visual-trash-assessments/edit/:${routeParams.onlandVisualTrashAssessmentID}`,
                title: "OVTAs",
                loadComponent: () =>
                    import("./pages/trash-module/ovta-workflow/trash-ovta-workflow-outlet/trash-ovta-workflow-outlet.component").then((m) => m.TrashOvtaWorkflowOutletComponent),
                children: [
                    { path: "", redirectTo: "instructions", pathMatch: "full" },
                    {
                        path: "instructions",
                        title: "Instructions",
                        loadComponent: () =>
                            import("./pages/trash-module/ovta-workflow/trash-ovta-instructions/trash-ovta-instructions.component").then((m) => m.TrashOvtaInstructionsComponent),
                        canActivate: [authGuardFn],
                    },
                    {
                        path: "initiate-ovta",
                        title: "Initiate OVTA",
                        loadComponent: () =>
                            import("./pages/trash-module/ovta-workflow/trash-initiate-ovta/trash-initiate-ovta.component").then((m) => m.TrashInitiateOvtaComponent),
                        canActivate: [authGuardFn],
                    },
                    {
                        path: "record-observations",
                        title: "Record Observations",
                        loadComponent: () =>
                            import("./pages/trash-module/ovta-workflow/trash-ovta-record-observations/trash-ovta-record-observations.component").then(
                                (m) => m.TrashOvtaRecordObservationsComponent
                            ),
                        canActivate: [authGuardFn],
                    },
                    {
                        path: "add-or-remove-parcels",
                        title: "Add or Remove Parcels",
                        loadComponent: () =>
                            import("./pages/trash-module/ovta-workflow/trash-ovta-add-remove-parcels/trash-ovta-add-remove-parcels.component").then(
                                (m) => m.TrashOvtaAddRemoveParcelsComponent
                            ),
                        canActivate: [authGuardFn],
                    },
                    {
                        path: "refine-assessment-area",
                        title: "Refine Assessment Area",
                        loadComponent: () =>
                            import("./pages/trash-module/ovta-workflow/trash-ovta-refine-assessment-area/trash-ovta-refine-assessment-area.component").then(
                                (m) => m.TrashOvtaRefineAssessmentAreaComponent
                            ),
                        canActivate: [authGuardFn],
                    },
                    {
                        path: "review-and-finalize",
                        title: "Review and Finalize",
                        loadComponent: () =>
                            import("./pages/trash-module/ovta-workflow/trash-ovta-review-and-finalize/trash-ovta-review-and-finalize.component").then(
                                (m) => m.TrashOvtaReviewAndFinalizeComponent
                            ),
                        canActivate: [authGuardFn],
                    },
                ],
            },

            {
                path: "onland-visual-trash-assessment-areas",
                title: "OVTA Areas",
                children: [
                    {
                        path: "",
                        loadComponent: () => import("./pages/trash-module/ovtas/trash-ovta-area-index/trash-ovta-area-index.component").then((m) => m.TrashOvtaAreaIndexComponent),
                        canActivate: [authGuardFn],
                    },
                    {
                        path: `:${routeParams.onlandVisualTrashAssessmentAreaID}`,
                        loadComponent: () =>
                            import("./pages/trash-module/ovtas/trash-ovta-area-detail/trash-ovta-area-detail.component").then((m) => m.TrashOvtaAreaDetailComponent),
                        canActivate: [authGuardFn],
                    },
                    {
                        path: `:${routeParams.onlandVisualTrashAssessmentAreaID}/edit-location`,
                        loadComponent: () =>
                            import("./pages/trash-module/ovtas/trash-ovta-area-edit-location/trash-ovta-area-edit-location.component").then(
                                (m) => m.TrashOvtaAreaEditLocationComponent
                            ),
                        canActivate: [authGuardFn],
                    },
                ],
            },
            {
                path: "trash-analysis-areas",
                title: "Trash Analysis Areas",
                loadComponent: () =>
                    import("./pages/trash-module/trash-trash-generating-unit-index/trash-trash-generating-unit-index.component").then(
                        (m) => m.TrashTrashGeneratingUnitIndexComponent
                    ),
                canActivate: [authGuardFn],
            },
        ],
    },
    {
        path: ``,
        title: "Stormwater Tools",
        loadComponent: () => import("./pages/site-layout/site-layout.component").then((m) => m.SiteLayoutComponent),
        children: [
            { path: "", loadComponent: () => import("./pages/home/home-index/home-index.component").then((m) => m.HomeIndexComponent) },
            { path: "about", loadComponent: () => import("./pages/about/about.component").then((m) => m.AboutComponent) },
            { path: "modeling", loadComponent: () => import("./pages/modeling-about/modeling-about.component").then((m) => m.ModelingAboutComponent) },
            {
                path: `labels-and-definitions/:${routeParams.definitionID}`,
                loadComponent: () => import("./pages/field-definition-edit/field-definition-edit.component").then((m) => m.FieldDefinitionEditComponent),
                canActivate: [authGuardFn, ManagerOnlyGuard],
            },
            { path: "users", title: "Users", loadComponent: () => import("./pages/users/users.component").then((m) => m.UsersComponent) },
            {
                path: "organizations",
                title: "Organizations",
                loadComponent: () => import("./pages/organizations/organizations.component").then((m) => m.OrganizationsComponent),
            },
            {
                path: "labels-and-definitions",
                title: "Labels and Definitions",
                loadComponent: () => import("./pages/field-definition-list/field-definition-list.component").then((m) => m.FieldDefinitionListComponent),
                canActivate: [authGuardFn, ManagerOnlyGuard],
            },
            {
                path: "jurisdictions",
                title: "Jurisdictions",
                loadComponent: () => import("./pages/jurisdictions/jurisdictions.component").then((m) => m.JurisdictionsComponent),
            },
            {
                path: `jurisdictions/:${routeParams.jurisdictionID}`,
                title: "Jurisdiction Detail",
                loadComponent: () => import("./pages/jurisdictions/jurisdiction-detail/jurisdiction-detail.component").then((m) => m.JurisdictionDetailComponent),
            },
            {
                path: "find-bmp",
                title: "Find a BMP",
                loadComponent: () => import("./pages/find-bmp/find-bmp.component").then((m) => m.FindBmpComponent),
            },
            {
                path: "modeling-parameters",
                title: "Modeling Parameters",
                loadComponent: () => import("./pages/modeling-parameters/modeling-parameters.component").then((m) => m.ModelingParametersComponent),
            },
            {
                path: "treatment-bmps",
                title: "View All BMPs",
                loadComponent: () => import("./pages/treatment-bmps/treatment-bmps.component").then((m) => m.TreatmentBmpsComponent),
            },
            {
                path: "treatment-bmps/new",
                title: "Create New BMP",
                loadComponent: () => import("./pages/treatment-bmps/create-treatment-bmp/create-treatment-bmp.component").then((m) => m.CreateTreatmentBmpComponent),
                canDeactivate: [UnsavedChangesGuard],
            },
            {
                path: `treatment-bmps/:${routeParams.treatmentBMPID}`,
                title: "Treatment BMP Detail",
                loadComponent: () => import("./pages/treatment-bmps/treatment-bmp-detail/treatment-bmp-detail.component").then((m) => m.TreatmentBmpDetailComponent),
            },
            {
                path: `treatment-bmps/:${routeParams.treatmentBMPID}/edit-basic-info`,
                title: "Edit BMP Basic Info",
                loadComponent: () =>
                    import("./pages/treatment-bmps/treatment-bmp-detail/treatment-bmp-update-basic-info/treatment-bmp-update-basic-info.component").then(
                        (m) => m.TreatmentBmpUpdateBasicInfoComponent
                    ),
                canDeactivate: [UnsavedChangesGuard],
            },
            {
                path: `treatment-bmps/:${routeParams.treatmentBMPID}/edit-images`,
                title: "Edit BMP Images",
                loadComponent: () =>
                    import("./pages/treatment-bmps/treatment-bmp-detail/treatment-bmp-update-images/treatment-bmp-update-images.component").then(
                        (m) => m.TreatmentBmpUpdateImagesComponent
                    ),
                canDeactivate: [UnsavedChangesGuard],
            },
            {
                path: `treatment-bmps/:${routeParams.treatmentBMPID}/edit-location`,
                title: "Edit BMP Location",
                loadComponent: () =>
                    import("./pages/treatment-bmps/treatment-bmp-detail/treatment-bmp-update-location/treatment-bmp-update-location.component").then(
                        (m) => m.TreatmentBmpUpdateLocationComponent
                    ),
                canDeactivate: [UnsavedChangesGuard],
            },
            {
                path: `treatment-bmps/:${routeParams.treatmentBMPID}/edit-custom-attributes/:${routeParams.customAttributePurposeID}`,
                title: "Edit BMP Custom Attributes",
                loadComponent: () =>
                    import("./pages/treatment-bmps/treatment-bmp-detail/treatment-bmp-update-custom-attributes/treatment-bmp-update-custom-attributes.component").then(
                        (m) => m.TreatmentBmpUpdateCustomAttributesComponent
                    ),
                canDeactivate: [UnsavedChangesGuard],
            },
            {
                path: "latest-bmp-assessments",
                title: "View Latest BMP Assessments",
                loadComponent: () => import("./pages/latest-bmp-assessments/latest-bmp-assessments.component").then((m) => m.LatestBmpAssessmentsComponent),
            },
            {
                path: "field-records",
                title: "View All Field Records",
                loadComponent: () => import("./pages/field-records/field-records.component").then((m) => m.FieldRecordsComponent),
            },
            {
                path: "water-quality-management-plans",
                title: "Water Quality Management Plans",
                loadComponent: () => import("./pages/wqmps/wqmps.component").then((m) => m.WqmpsComponent),
            },
            {
                path: `water-quality-management-plans/:${routeParams.waterQualityManagementPlanID}`,
                title: "WQMP Detail",
                loadComponent: () => import("./pages/wqmps/wqmp-detail/wqmp-detail.component").then((m) => m.WqmpDetailComponent),
            },
            {
                path: `water-quality-management-plans/:${routeParams.waterQualityManagementPlanID}/edit-source-control-bmps`,
                title: "Edit Source Control BMPs",
                loadComponent: () =>
                    import("./pages/wqmps/wqmp-detail/edit-source-control-bmps/edit-source-control-bmps.component").then((m) => m.EditSourceControlBMPsComponent),
                canActivate: [authGuardFn],
            },
            {
                path: `water-quality-management-plans/:${routeParams.waterQualityManagementPlanID}/edit-quick-bmps`,
                title: "Edit Simplified Structural BMPs",
                loadComponent: () =>
                    import("./pages/wqmps/wqmp-detail/edit-quick-bmps/edit-quick-bmps.component").then((m) => m.EditQuickBMPsComponent),
                canActivate: [authGuardFn],
            },
            {
                path: `water-quality-management-plans/:${routeParams.waterQualityManagementPlanID}/edit-boundary`,
                title: "Refine WQMP Boundary Area",
                loadComponent: () =>
                    import("./pages/wqmps/wqmp-detail/edit-boundary/edit-boundary.component").then((m) => m.EditBoundaryComponent),
                canActivate: [authGuardFn],
            },
            {
                path: `water-quality-management-plans/:${routeParams.waterQualityManagementPlanID}/edit-parcels`,
                title: "Add or Remove Parcels",
                loadComponent: () =>
                    import("./pages/wqmps/wqmp-detail/edit-parcels/edit-parcels.component").then((m) => m.EditParcelsComponent),
                canActivate: [authGuardFn],
            },
            {
                path: "wqmp-annual-report",
                title: "WQMP Annual Report",
                loadComponent: () => import("./pages/wqmp-annual-report/wqmp-annual-report.component").then((m) => m.WqmpAnnualReportComponent),
            },
            {
                path: "parcels",
                title: "Parcels",
                loadComponent: () => import("./pages/parcels/parcels.component").then((m) => m.ParcelsComponent),
            },
            // Program Info
            {
                path: "program-info/observation-types",
                title: "Observation Types",
                loadComponent: () => import("./pages/program-info/observation-types.component").then((m) => m.ObservationTypesComponent),
            },
            {
                path: "program-info/treatment-bmp-types",
                title: "Treatment BMP Types",
                loadComponent: () => import("./pages/program-info/treatment-bmp-types.component").then((m) => m.TreatmentBmpTypesComponent),
            },
            {
                path: "funding-sources",
                title: "Funding Sources",
                loadComponent: () => import("./pages/funding-sources/funding-sources.component").then((m) => m.FundingSourcesComponent),
            },
            // Dashboard
            {
                path: "dashboard",
                title: "Dashboard",
                loadComponent: () => import("./pages/dashboard/dashboard.component").then((m) => m.DashboardComponent),
            },
            // Delineation
            {
                path: "delineation/delineation-map",
                title: "Delineation Map",
                loadComponent: () => import("./pages/delineation/delineation-map.component").then((m) => m.DelineationMapComponent),
            },
            {
                path: "delineation/delineation-reconciliation-report",
                title: "Delineation Reconciliation Report",
                loadComponent: () => import("./pages/delineation/delineation-reconciliation-report.component").then((m) => m.DelineationReconciliationReportComponent),
            },
            // Data Hub
            { path: "data-hub", title: "Data Hub", loadComponent: () => import("./pages/data-hub/data-hub.component").then((m) => m.DataHubComponent) },
            // Manage
            {
                path: "manage/homepage-configuration",
                title: "Homepage Configuration",
                loadComponent: () => import("./pages/manage/homepage-configuration.component").then((m) => m.HomepageConfigurationComponent),
            },
            {
                path: "manage/custom-attributes",
                title: "Custom Attributes",
                loadComponent: () => import("./pages/manage/custom-attributes.component").then((m) => m.CustomAttributesComponent),
            },
            {
                path: "load-generating-units",
                title: "Load Generating Units",
                loadComponent: () => import("./pages/load-generating-units/load-generating-units.component").then((m) => m.LoadGeneratingUnitsComponent),
            },
            {
                path: `load-generating-units/:${routeParams.loadGeneratingUnitID}`,
                title: "Load Generating Unit Detail",
                loadComponent: () =>
                    import("./pages/load-generating-units/load-generating-unit-detail/load-generating-unit-detail.component").then((m) => m.LoadGeneratingUnitDetailComponent),
            },
            {
                path: "hru-characteristics",
                title: "HRU Characteristics",
                loadComponent: () => import("./pages/hru-characteristics/hru-characteristics.component").then((m) => m.HRUCharacteristicsComponent),
            },
            {
                path: "regional-subbasins",
                title: "Regional Subbasins",
                loadComponent: () => import("./pages/regional-subbasins/regional-subbasins.component").then((m) => m.RegionalSubbasinsComponent),
            },
            {
                path: `regional-subbasins/:${routeParams.regionalSubbasinID}`,
                title: "Regional Subbasin Detail",
                loadComponent: () =>
                    import("./pages/regional-subbasins/regional-subbasin-detail/regional-subbasin-detail.component").then((m) => m.RegionalSubbasinDetailComponent),
            },
            {
                path: "manage/regional-subbasin-revision-requests",
                title: "Regional Subbasin Revision Requests",
                loadComponent: () => import("./pages/manage/regional-subbasin-revision-requests.component").then((m) => m.RegionalSubbasinRevisionRequestsComponent),
            },
            {
                path: "manage/wqmp-lgu-audit",
                title: "Water Quality Management Plan LGU Audit",
                loadComponent: () => import("./pages/manage/wqmp-lgu-audit.component").then((m) => m.WqmpLguAuditComponent),
            },
        ],
    },
    { path: "not-found", loadComponent: () => import("./shared/pages").then((m) => m.NotFoundComponent) },
    { path: "subscription-insufficient", loadComponent: () => import("./shared/pages/").then((m) => m.SubscriptionInsufficientComponent) },
    { path: "unauthenticated", loadComponent: () => import("./shared/pages").then((m) => m.UnauthenticatedComponent) },
    { path: "callback", component: AuthCallbackComponent },
    { path: "**", loadComponent: () => import("./shared/pages").then((m) => m.NotFoundComponent) },
];
