import { Routes } from "@angular/router";
import { ManagerOnlyGuard } from "./shared/guards/unauthenticated-access/manager-only-guard";
import { ManagerOrAdminOnlyGuard } from "./shared/guards/unauthenticated-access/manager-or-admin-only-guard";
import { JurisdictionManagerOrEditorOnlyGuard } from "./shared/guards/unauthenticated-access/jurisdiction-manager-or-editor-only-guard.guard";
import { UnsavedChangesGuard } from "./shared/guards/unsaved-changes.guard";
import { OCTAGrantReviewerOnlyGuard } from "./shared/guards/unauthenticated-access/octa-grant-reviewer-only.guard";
import { authGuardFn } from "@auth0/auth0-angular";
import { AuthCallbackComponent } from "./auth-callback.component";

export const routeParams = {
    definitionID: "definitionID",
    fieldDefinitionTypeID: "fieldDefinitionTypeID",
    fundingSourceID: "fundingSourceID",
    organizationID: "organizationID",
    projectID: "projectID",
    onlandVisualTrashAssessmentID: "onlandVisualTrashAssessmentID",
    onlandVisualTrashAssessmentAreaID: "onlandVisualTrashAssessmentAreaID",
    treatmentBMPID: "treatmentBMPID",
    loadGeneratingUnitID: "loadGeneratingUnitID",
    jurisdictionID: "jurisdictionID",
    regionalSubbasinID: "regionalSubbasinID",
    customAttributePurposeID: "customAttributePurposeID",
    waterQualityManagementPlanID: "waterQualityManagementPlanID",
    waterQualityManagementPlanVerifyID: "waterQualityManagementPlanVerifyID",
    treatmentBMPTypeID: "treatmentBMPTypeID",
    customAttributeTypeID: "customAttributeTypeID",
    observationTypeID: "observationTypeID",
    fieldVisitID: "fieldVisitID",
    treatmentBMPAssessmentID: "treatmentBMPAssessmentID",
    maintenanceRecordID: "maintenanceRecordID",
    personID: "personID",
};

// Anonymous-friendly routes (e.g., /support) live under the public site layout below alongside
// auth'd ones; they intentionally have no canActivate so unauthenticated visitors can reach them.

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
                        title: "Select Assessment Area",
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
            {
                path: "training",
                title: "Training",
                loadComponent: () => import("./pages/training/training.component").then((m) => m.TrainingComponent),
            },
            { path: "modeling", loadComponent: () => import("./pages/modeling-about/modeling-about.component").then((m) => m.ModelingAboutComponent) },
            {
                // NPT-999: Canonical edit URL per AC. Reads `fieldDefinitionTypeID` from the param map.
                path: `field-definitions/:${routeParams.fieldDefinitionTypeID}/edit`,
                loadComponent: () => import("./pages/field-definition-edit/field-definition-edit.component").then((m) => m.FieldDefinitionEditComponent),
                canActivate: [authGuardFn, ManagerOnlyGuard],
            },
            {
                // Legacy URL kept as a redirect so any persisted links (or the prior list-cell
                // routing target before this rework) still land on the new canonical URL.
                path: `labels-and-definitions/:${routeParams.definitionID}`,
                redirectTo: `field-definitions/:${routeParams.definitionID}/edit`,
                pathMatch: "full",
            },
            {
                // NPT-999 r3 (KE 5/20/26): the users list is Admin / SitkaAdmin only.
                // Jurisdiction users have no business seeing the full user roster; they
                // reach their own profile via the Welcome dropdown's Account link.
                path: "users",
                title: "Users",
                loadComponent: () => import("./pages/users/users.component").then((m) => m.UsersComponent),
                canActivate: [authGuardFn, ManagerOnlyGuard],
            },
            {
                path: `users/:${routeParams.personID}`,
                title: "User Detail",
                loadComponent: () => import("./pages/users/user-detail/user-detail.component").then((m) => m.UserDetailComponent),
                canActivate: [authGuardFn],
            },
            {
                path: "support",
                title: "Request Support",
                loadComponent: () => import("./pages/support/request-support/request-support.component").then((m) => m.RequestSupportComponent),
            },
            {
                path: "organizations",
                title: "Organizations",
                loadComponent: () => import("./pages/organizations/organizations.component").then((m) => m.OrganizationsComponent),
            },
            {
                // NPT-999: org detail page. Backend GET is UserViewFeature (any authenticated
                // user — matches the legacy MVC OrganizationViewFeature). The component's 403
                // handler redirects to /organizations if a future tightening ever blocks a
                // viewer. Linked from the User Detail page's Role / Organization and Primary
                // Contact Organizations sections, plus several grids and the FS detail page.
                path: `organizations/:${routeParams.organizationID}`,
                title: "Organization Detail",
                loadComponent: () => import("./pages/organizations/organization-detail/organization-detail.component").then((m) => m.OrganizationDetailComponent),
                canActivate: [authGuardFn],
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
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: `jurisdictions/:${routeParams.jurisdictionID}`,
                title: "Jurisdiction Detail",
                loadComponent: () => import("./pages/jurisdictions/jurisdiction-detail/jurisdiction-detail.component").then((m) => m.JurisdictionDetailComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
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
                // NPT-984: dedicated read-only Field Visit detail page. The Field Records grid
                // routes here for any visit not in InProgress; the workflow outlet's Wrap Up
                // handler navigates here after finalize. Keeps editable workflow pages and the
                // locked-down summary view as separate routes so wrap-up actually wraps up.
                path: `field-visits/:${routeParams.fieldVisitID}/view`,
                title: "Field Visit",
                loadComponent: () =>
                    import("./pages/field-visits/field-visit-detail-readonly/field-visit-detail-readonly.component").then(
                        (m) => m.FieldVisitDetailReadOnlyComponent
                    ),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                // NPT-1056: SPA detail page for a single Treatment BMP Assessment — ports
                // the legacy MVC `/TreatmentBMPAssessment/Detail/{id}` view. Manager Dashboard
                // Field Visits tab now deep-links here for the Initial / Post-Maintenance
                // Assessment grid columns instead of bouncing the user back to MVC.
                path: `treatment-bmp-assessments/:${routeParams.treatmentBMPAssessmentID}`,
                title: "Treatment BMP Assessment",
                loadComponent: () =>
                    import("./pages/treatment-bmp-assessments/treatment-bmp-assessment-detail/treatment-bmp-assessment-detail.component").then(
                        (m) => m.TreatmentBmpAssessmentDetailComponent
                    ),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                // NPT-1056: SPA detail page for a single Maintenance Record — ports the legacy
                // MVC `/MaintenanceRecord/Detail/{id}` view. Manager Dashboard Field Visits tab
                // deep-links here for the "Maintenance Occurred" column.
                path: `maintenance-records/:${routeParams.maintenanceRecordID}`,
                title: "Maintenance Record",
                loadComponent: () =>
                    import("./pages/maintenance-records/maintenance-record-detail/maintenance-record-detail.component").then(
                        (m) => m.MaintenanceRecordDetailComponent
                    ),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: `field-visits/:${routeParams.fieldVisitID}`,
                title: "Field Visit",
                loadComponent: () =>
                    import("./pages/field-visits/field-visit-workflow-outlet/field-visit-workflow-outlet.component").then(
                        (m) => m.FieldVisitWorkflowOutletComponent
                    ),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
                children: [
                    { path: "", redirectTo: "inventory", pathMatch: "full" },
                    {
                        path: "inventory",
                        title: "Inventory",
                        loadComponent: () =>
                            import("./pages/field-visits/field-visit-workflow-outlet/inventory-step/inventory-step.component").then(
                                (m) => m.FieldVisitInventoryStepComponent
                            ),
                    },
                    {
                        path: "inventory/location",
                        title: "Inventory — Location",
                        loadComponent: () =>
                            import("./pages/field-visits/field-visit-workflow-outlet/inventory-location-step/inventory-location-step.component").then(
                                (m) => m.FieldVisitInventoryLocationStepComponent
                            ),
                    },
                    {
                        path: "inventory/photos",
                        title: "Inventory — Photos",
                        loadComponent: () =>
                            import("./pages/field-visits/field-visit-workflow-outlet/inventory-photos-step/inventory-photos-step.component").then(
                                (m) => m.FieldVisitInventoryPhotosStepComponent
                            ),
                    },
                    {
                        path: "inventory/attributes",
                        title: "Inventory — Attributes",
                        loadComponent: () =>
                            import("./pages/field-visits/field-visit-workflow-outlet/inventory-attributes-step/inventory-attributes-step.component").then(
                                (m) => m.FieldVisitInventoryAttributesStepComponent
                            ),
                    },
                    {
                        path: "assessment",
                        title: "Initial Assessment",
                        loadComponent: () =>
                            import("./pages/field-visits/field-visit-workflow-outlet/assessment-step/assessment-step.component").then(
                                (m) => m.FieldVisitAssessmentStepComponent
                            ),
                        // withComponentInputBinding() overrides @Input defaults with `undefined` when the route has
                        // no matching data key — must explicitly set `assessmentTypeID: 1` here so the Initial
                        // assessment landing page (and observations/photos sub-steps) get the right type.
                        data: { assessmentTypeID: 1 },
                    },
                    {
                        path: "assessment/observations",
                        title: "Initial Assessment Observations",
                        loadComponent: () =>
                            import("./pages/field-visits/field-visit-workflow-outlet/observations-step/observations-step.component").then(
                                (m) => m.FieldVisitObservationsStepComponent
                            ),
                        data: { assessmentTypeID: 1 },
                    },
                    {
                        path: "assessment/photos",
                        title: "Initial Assessment Photos",
                        loadComponent: () =>
                            import("./pages/field-visits/field-visit-workflow-outlet/assessment-photos-step/assessment-photos-step.component").then(
                                (m) => m.FieldVisitAssessmentPhotosStepComponent
                            ),
                        data: { assessmentTypeID: 1 },
                    },
                    {
                        path: "maintenance",
                        title: "Maintenance",
                        loadComponent: () =>
                            import("./pages/field-visits/field-visit-workflow-outlet/maintenance-step/maintenance-step.component").then(
                                (m) => m.FieldVisitMaintenanceStepComponent
                            ),
                    },
                    {
                        path: "maintenance/edit",
                        title: "Edit Maintenance Record",
                        loadComponent: () =>
                            import("./pages/field-visits/field-visit-workflow-outlet/maintenance-edit-step/maintenance-edit-step.component").then(
                                (m) => m.FieldVisitMaintenanceEditStepComponent
                            ),
                    },
                    {
                        path: "post-maintenance-assessment",
                        title: "Post-Maintenance Assessment",
                        loadComponent: () =>
                            import(
                                "./pages/field-visits/field-visit-workflow-outlet/post-maintenance-assessment-step/post-maintenance-assessment-step.component"
                            ).then((m) => m.FieldVisitPostMaintenanceAssessmentStepComponent),
                    },
                    {
                        path: "post-maintenance-assessment/observations",
                        title: "Post-Maintenance Observations",
                        loadComponent: () =>
                            import("./pages/field-visits/field-visit-workflow-outlet/observations-step/observations-step.component").then(
                                (m) => m.FieldVisitObservationsStepComponent
                            ),
                        data: { assessmentTypeID: 2 },
                    },
                    {
                        path: "post-maintenance-assessment/photos",
                        title: "Post-Maintenance Photos",
                        loadComponent: () =>
                            import("./pages/field-visits/field-visit-workflow-outlet/assessment-photos-step/assessment-photos-step.component").then(
                                (m) => m.FieldVisitAssessmentPhotosStepComponent
                            ),
                        data: { assessmentTypeID: 2 },
                    },
                    {
                        path: "summary",
                        title: "Visit Summary",
                        loadComponent: () =>
                            import("./pages/field-visits/field-visit-workflow-outlet/visit-summary-step/visit-summary-step.component").then(
                                (m) => m.FieldVisitVisitSummaryStepComponent
                            ),
                    },
                ],
            },
            {
                path: "water-quality-management-plans",
                title: "Water Quality Management Plans",
                loadComponent: () => import("./pages/wqmps/wqmps.component").then((m) => m.WqmpsComponent),
            },
            {
                path: "water-quality-management-plan-verifications",
                title: "WQMP O&M Verifications",
                loadComponent: () => import("./pages/wqmps/wqmp-verifications/wqmp-verifications.component").then((m) => m.WqmpVerificationsComponent),
                canActivate: [authGuardFn],
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
                path: `water-quality-management-plans/:${routeParams.waterQualityManagementPlanID}/verifications/new`,
                title: "New O&M Verification",
                loadComponent: () =>
                    import("./pages/wqmps/wqmp-detail/verification-wizard/verification-wizard-outlet.component").then((m) => m.VerificationWizardOutletComponent),
                canActivate: [authGuardFn],
                children: [
                    { path: "", redirectTo: "basics", pathMatch: "full" },
                    {
                        path: "basics",
                        loadComponent: () =>
                            import("./pages/wqmps/wqmp-detail/verification-wizard/steps/verification-basics-step.component").then((m) => m.VerificationBasicsStepComponent),
                    },
                    {
                        path: "structural-bmps",
                        loadComponent: () =>
                            import("./pages/wqmps/wqmp-detail/verification-wizard/steps/structural-bmps-step.component").then((m) => m.StructuralBmpsStepComponent),
                    },
                    {
                        path: "simplified-bmps",
                        loadComponent: () =>
                            import("./pages/wqmps/wqmp-detail/verification-wizard/steps/simplified-bmps-step.component").then((m) => m.SimplifiedBmpsStepComponent),
                    },
                    {
                        path: "source-control",
                        loadComponent: () =>
                            import("./pages/wqmps/wqmp-detail/verification-wizard/steps/source-control-step.component").then((m) => m.SourceControlStepComponent),
                    },
                    {
                        path: "supporting-documentation",
                        loadComponent: () =>
                            import("./pages/wqmps/wqmp-detail/verification-wizard/steps/supporting-documentation-step.component").then((m) => m.SupportingDocumentationStepComponent),
                    },
                    {
                        path: "review-and-finalize",
                        loadComponent: () =>
                            import("./pages/wqmps/wqmp-detail/verification-wizard/steps/review-step.component").then((m) => m.ReviewStepComponent),
                    },
                ],
            },
            {
                path: `water-quality-management-plans/:${routeParams.waterQualityManagementPlanID}/verifications/:${routeParams.waterQualityManagementPlanVerifyID}`,
                title: "O&M Verification",
                loadComponent: () =>
                    import("./pages/wqmps/wqmp-detail/verification-wizard/verification-wizard-outlet.component").then((m) => m.VerificationWizardOutletComponent),
                canActivate: [authGuardFn],
                children: [
                    { path: "", redirectTo: "basics", pathMatch: "full" },
                    {
                        path: "basics",
                        loadComponent: () =>
                            import("./pages/wqmps/wqmp-detail/verification-wizard/steps/verification-basics-step.component").then((m) => m.VerificationBasicsStepComponent),
                    },
                    {
                        path: "structural-bmps",
                        loadComponent: () =>
                            import("./pages/wqmps/wqmp-detail/verification-wizard/steps/structural-bmps-step.component").then((m) => m.StructuralBmpsStepComponent),
                    },
                    {
                        path: "simplified-bmps",
                        loadComponent: () =>
                            import("./pages/wqmps/wqmp-detail/verification-wizard/steps/simplified-bmps-step.component").then((m) => m.SimplifiedBmpsStepComponent),
                    },
                    {
                        path: "source-control",
                        loadComponent: () =>
                            import("./pages/wqmps/wqmp-detail/verification-wizard/steps/source-control-step.component").then((m) => m.SourceControlStepComponent),
                    },
                    {
                        path: "supporting-documentation",
                        loadComponent: () =>
                            import("./pages/wqmps/wqmp-detail/verification-wizard/steps/supporting-documentation-step.component").then((m) => m.SupportingDocumentationStepComponent),
                    },
                    {
                        path: "review-and-finalize",
                        loadComponent: () =>
                            import("./pages/wqmps/wqmp-detail/verification-wizard/steps/review-step.component").then((m) => m.ReviewStepComponent),
                    },
                ],
            },
            {
                path: `water-quality-management-plans/:${routeParams.waterQualityManagementPlanID}/verifications/:${routeParams.waterQualityManagementPlanVerifyID}/view`,
                title: "O&M Verification Detail",
                loadComponent: () =>
                    import("./pages/wqmps/wqmp-detail/verification-detail/verification-detail.component").then((m) => m.VerificationDetailComponent),
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
                path: `water-quality-management-plans/:${routeParams.waterQualityManagementPlanID}/review`,
                title: "WQMP AI Review",
                loadComponent: () =>
                    import("./pages/wqmps/wqmp-detail/wqmp-review/wqmp-review.component").then((m) => m.WqmpReviewComponent),
                canActivate: [authGuardFn],
                canDeactivate: [UnsavedChangesGuard],
            },
            {
                path: "wqmp-annual-report",
                title: "WQMP Annual Report",
                loadComponent: () => import("./pages/wqmp-annual-report/wqmp-annual-report.component").then((m) => m.WqmpAnnualReportComponent),
                canActivate: [authGuardFn, JurisdictionManagerOrEditorOnlyGuard],
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
                path: "program-info/observation-types/:observationTypeID",
                title: "Observation Type Detail",
                loadComponent: () => import("./pages/program-info/observation-type-detail/observation-type-detail.component").then((m) => m.ObservationTypeDetailComponent),
            },
            {
                path: "program-info/treatment-bmp-types",
                title: "Treatment BMP Types",
                loadComponent: () => import("./pages/program-info/treatment-bmp-types.component").then((m) => m.TreatmentBmpTypesComponent),
            },
            {
                path: "program-info/treatment-bmp-types/:treatmentBMPTypeID",
                title: "Treatment BMP Type Detail",
                loadComponent: () => import("./pages/program-info/treatment-bmp-type-detail/treatment-bmp-type-detail.component").then((m) => m.TreatmentBmpTypeDetailComponent),
            },
            {
                path: "funding-sources",
                title: "Funding Sources",
                loadComponent: () => import("./pages/funding-sources/funding-sources.component").then((m) => m.FundingSourcesComponent),
            },
            {
                // NPT-999: SPA Funding Source detail page mirroring the legacy MVC view.
                // Linked from the Organization detail page's Funding Sources panel.
                path: `funding-sources/:${routeParams.fundingSourceID}`,
                title: "Funding Source Detail",
                loadComponent: () => import("./pages/funding-sources/funding-source-detail/funding-source-detail.component").then((m) => m.FundingSourceDetailComponent),
                canActivate: [authGuardFn],
            },
            // Dashboard (Manager Dashboard) — Admin / SitkaAdmin / JurisdictionManager only
            {
                path: "dashboard",
                title: "Dashboard",
                loadComponent: () => import("./pages/dashboard/dashboard.component").then((m) => m.DashboardComponent),
                canActivate: [authGuardFn, ManagerOrAdminOnlyGuard],
            },
            // Delineation
            {
                path: "delineation/delineation-map",
                title: "Delineation Map",
                loadComponent: () => import("./pages/delineation/delineation-map/delineation-map.component").then((m) => m.DelineationMapComponent),
            },
            {
                path: "delineation/delineation-reconciliation-report",
                title: "Delineation Reconciliation Report",
                loadComponent: () =>
                    import("./pages/delineation/delineation-reconciliation-report/delineation-reconciliation-report.component").then(
                        (m) => m.DelineationReconciliationReportComponent
                    ),
                canActivate: [authGuardFn, JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "delineation/revision-requests",
                title: "Regional Subbasin Revision Requests",
                loadComponent: () =>
                    import("./pages/delineation/revision-requests/revision-requests.component").then((m) => m.RevisionRequestsComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "delineation/revision-requests/new/:treatmentBMPID",
                title: "New Revision Request",
                loadComponent: () =>
                    import("./pages/delineation/revision-requests/revision-request-new.component").then((m) => m.RevisionRequestNewComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "delineation/revision-requests/:regionalSubbasinRevisionRequestID",
                title: "Revision Request Detail",
                loadComponent: () =>
                    import("./pages/delineation/revision-requests/revision-request-detail.component").then((m) => m.RevisionRequestDetailComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "delineation/gdb-upload",
                title: "Upload Delineations",
                loadComponent: () => import("./pages/delineation/gdb-upload/gdb-upload.component").then((m) => m.GdbUploadComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "delineation/gdb-approve",
                title: "Approve Uploaded Delineations",
                loadComponent: () => import("./pages/delineation/gdb-approve/gdb-approve.component").then((m) => m.GdbApproveComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "delineation/gdb-download",
                title: "Download Delineations",
                loadComponent: () => import("./pages/delineation/gdb-download/gdb-download.component").then((m) => m.GdbDownloadComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            // Data Hub
            {
                path: "data-hub",
                title: "Data Hub",
                loadComponent: () => import("./pages/data-hub/data-hub.component").then((m) => m.DataHubComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "data-hub/treatment-bmp-upload",
                title: "Upload Treatment BMPs",
                loadComponent: () => import("./pages/data-hub/treatment-bmp-upload/treatment-bmp-upload.component").then((m) => m.TreatmentBMPUploadComponent),
                canActivate: [ManagerOnlyGuard],
            },
            {
                path: "data-hub/treatment-bmp-download",
                title: "Download Treatment BMPs",
                loadComponent: () => import("./pages/data-hub/treatment-bmp-download/treatment-bmp-download.component").then((m) => m.TreatmentBMPDownloadComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "data-hub/trash-screen-field-visit-upload",
                title: "Upload Trash Screen Field Visits",
                loadComponent: () =>
                    import("./pages/data-hub/trash-screen-field-visit-upload/trash-screen-field-visit-upload.component").then((m) => m.TrashScreenFieldVisitUploadComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "data-hub/wqmp-upload",
                title: "Upload Water Quality Management Plans",
                loadComponent: () => import("./pages/data-hub/wqmp-upload/wqmp-upload.component").then((m) => m.WqmpUploadComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "data-hub/simplified-bmp-upload",
                title: "Upload Simplified BMPs",
                loadComponent: () => import("./pages/data-hub/simplified-bmp-upload/simplified-bmp-upload.component").then((m) => m.SimplifiedBmpUploadComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "data-hub/wqmp-locations-upload",
                title: "Upload WQMP Locations",
                loadComponent: () => import("./pages/data-hub/wqmp-locations-upload/wqmp-locations-upload.component").then((m) => m.WqmpLocationsUploadComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "data-hub/ovta-area-upload",
                title: "Upload OVTA Areas",
                loadComponent: () => import("./pages/data-hub/ovta-area-upload/ovta-area-upload.component").then((m) => m.OvtaAreaUploadComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "data-hub/ovta-area-approve",
                title: "Approve OVTA Areas",
                loadComponent: () => import("./pages/data-hub/ovta-area-approve/ovta-area-approve.component").then((m) => m.OvtaAreaApproveComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "data-hub/ovta-area-download",
                title: "Download OVTA Areas",
                loadComponent: () => import("./pages/data-hub/ovta-area-download/ovta-area-download.component").then((m) => m.OvtaAreaDownloadComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "data-hub/ovta-upload",
                title: "Upload OVTAs",
                loadComponent: () => import("./pages/data-hub/ovta-upload/ovta-upload.component").then((m) => m.OvtaUploadComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "data-hub/land-use-block-upload",
                title: "Upload Land Use Blocks",
                loadComponent: () => import("./pages/data-hub/land-use-block-upload/land-use-block-upload.component").then((m) => m.LandUseBlockUploadComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            {
                path: "data-hub/land-use-block-download",
                title: "Download Land Use Blocks",
                loadComponent: () => import("./pages/data-hub/land-use-block-download/land-use-block-download.component").then((m) => m.LandUseBlockDownloadComponent),
                canActivate: [JurisdictionManagerOrEditorOnlyGuard],
            },
            // Manage
            {
                path: "manage/custom-attributes",
                title: "Custom Attributes",
                loadComponent: () => import("./pages/manage/custom-attributes.component").then((m) => m.CustomAttributesComponent),
            },
            {
                path: "manage/custom-attributes/new",
                title: "New Custom Attribute",
                loadComponent: () => import("./pages/manage/custom-attribute-type-edit/custom-attribute-type-edit.component").then((m) => m.CustomAttributeTypeEditComponent),
            },
            {
                path: `manage/custom-attributes/:${routeParams.customAttributeTypeID}/edit`,
                title: "Edit Custom Attribute",
                loadComponent: () => import("./pages/manage/custom-attribute-type-edit/custom-attribute-type-edit.component").then((m) => m.CustomAttributeTypeEditComponent),
            },
            {
                path: `manage/custom-attributes/:${routeParams.customAttributeTypeID}`,
                title: "Custom Attribute Type Detail",
                loadComponent: () => import("./pages/manage/custom-attribute-type-detail/custom-attribute-type-detail.component").then((m) => m.CustomAttributeTypeDetailComponent),
            },
            {
                path: "manage/observation-types",
                title: "Observation Types",
                loadComponent: () => import("./pages/manage/observation-types-manage.component").then((m) => m.ObservationTypesManageComponent),
            },
            {
                path: "manage/observation-types/new",
                title: "New Observation Type",
                loadComponent: () => import("./pages/manage/observation-type-edit/observation-type-edit.component").then((m) => m.ObservationTypeEditComponent),
            },
            {
                path: `manage/observation-types/:${routeParams.observationTypeID}/edit`,
                title: "Edit Observation Type",
                loadComponent: () => import("./pages/manage/observation-type-edit/observation-type-edit.component").then((m) => m.ObservationTypeEditComponent),
            },
            {
                path: "manage/treatment-bmp-types",
                title: "Treatment BMP Types",
                loadComponent: () => import("./pages/manage/treatment-bmp-types-manage.component").then((m) => m.TreatmentBmpTypesManageComponent),
            },
            {
                path: "manage/treatment-bmp-types/new",
                title: "New Treatment BMP Type",
                loadComponent: () => import("./pages/manage/treatment-bmp-type-edit/treatment-bmp-type-edit.component").then((m) => m.TreatmentBmpTypeEditComponent),
            },
            {
                path: `manage/treatment-bmp-types/:${routeParams.treatmentBMPTypeID}/edit`,
                title: "Edit Treatment BMP Type",
                loadComponent: () => import("./pages/manage/treatment-bmp-type-edit/treatment-bmp-type-edit.component").then((m) => m.TreatmentBmpTypeEditComponent),
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
