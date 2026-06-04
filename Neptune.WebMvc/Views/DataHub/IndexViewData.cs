using Neptune.EFModels.Entities;
using Neptune.WebMvc.Common;
using Neptune.WebMvc.Controllers;
using Neptune.WebMvc.Security;
using Neptune.WebMvc.Views.Shared;

namespace Neptune.WebMvc.Views.DataHub
{
    public class IndexViewData : NeptuneViewData
    {
        public bool HasManagePermission { get; }
        public bool IsAdmin { get; }
        public bool IsManagerOrEditor { get; }
        public bool HasJurisdictionEditPermission { get; }
        public bool HasNeptuneViewAndRequiresJurisdictionsPermission { get; }
        public ViewPageContentViewData TreatmentBMPPage { get; }
        public ViewPageContentViewData DelineationPage { get; }
        public ViewPageContentViewData FieldTripPage { get; }
        public ViewPageContentViewData WQMPPage { get; }
        public ViewPageContentViewData SimplifiedBMPPage { get; }
        public ViewPageContentViewData WQMPLocationPage { get; }
        public ViewPageContentViewData AssessmentAreaPage { get; }
        public ViewPageContentViewData OVTAsPage { get; }
        public ViewPageContentViewData LandUseBlocksPage { get; }
        public ViewPageContentViewData ParcelPage { get; }
        public ViewPageContentViewData RegionalSubbasinsPage { get; }
        public ViewPageContentViewData LandUseStatisticsPage { get; }
        public ViewPageContentViewData ModelBasinsPage { get; }
        public ViewPageContentViewData PrecipitationZonesPage { get; }
        public string UploadTreatmentBMPUrl { get; }
        public string DownloadTreatmentBMPUrl { get; }
        public string UploadDelineationUrl { get; }
        public string DownloadDelineationUrl { get; }
        public string UploadFieldTripUrl { get; }
        public string UploadWQMPUrl { get; }
        public string UploadSimplifiedBMPUrl { get; }
        public string UploadWQMPLocationsUrl { get; }
        public string UploadAssessmentAreasUrl { get; }
        public string DownloadAssessmentAreasUrl { get; }
        public string UploadOVTAUrl { get; }
        public string UploadLandUseBlocksUrl { get; }
        public string DownloadLandUseBlocksUrl { get; }
        public string ParcelRefreshUrl { get; }
        public string RegionalSubbasinRefreshUrl { get; }
        public string LandUseStatisticsRefreshUrl { get; }
        public string ModelBasinsRefreshUrl { get; }
        public string PrecipitationZonesRefreshUrl { get; }
        public DateTime? LastUpdatedParcels { get; }
        public DateTime? LastUpdatedRegionalSubbasins { get; }
        public DateTime? LastUpdatedHRUCharacteristics { get; }
        public DateTime? LastUpdatedModalBasins { get; }
        public DateTime? LastUpdatedPrecipitationZones { get; }
        public string BMPListUrl { get; }
        public string FieldTripUrl { get; }
        public string ModelingAttributeUrl { get; }
        public string BMPMapUrl { get; }
        public string BMPTypesUrl { get; }
        public string WQMPListUrl { get; }
        public string OVTAListUrl { get; }
        public string TrashGeneratingUnitsAuditListUrl { get; }
        public string LandUseBlocksUrl { get; }
        public string ParcelMapUrl { get; }
        public string RSBListUrl { get; }
        public string RSBGridUrl { get; }
        public string HRUCharacteristicsListUrl { get; }

        public IndexViewData(HttpContext httpContext, LinkGenerator linkGenerator,
            WebConfiguration webConfiguration, Person currentPerson,
            DateTime? lastUpdatedParcels, DateTime? lastUpdatedRegionalSubbasins,
            DateTime? lastUpdatedDateModelBasins, DateTime? lastUpdatedDatePrecipitationZones,
            DateTime? lastUpdatedDateHRUCharacteristics, EFModels.Entities.NeptunePage treatmentBMPPage, EFModels.Entities.NeptunePage delineationPage,
            EFModels.Entities.NeptunePage fieldTripPage, EFModels.Entities.NeptunePage wqmpPage, 
            EFModels.Entities.NeptunePage simplifiedBMPPage, EFModels.Entities.NeptunePage wqmpLocationPage, 
            EFModels.Entities.NeptunePage assessmentAreaPage, EFModels.Entities.NeptunePage ovtasPage,
            EFModels.Entities.NeptunePage landUseBlocksPage, EFModels.Entities.NeptunePage parcelPage,
            EFModels.Entities.NeptunePage regionalSubbasinsPage, EFModels.Entities.NeptunePage landUseStatisticsPage,
            EFModels.Entities.NeptunePage modelBasinsPage, EFModels.Entities.NeptunePage precipitationZonesPage)
            : base(httpContext, linkGenerator, currentPerson, NeptuneArea.OCStormwaterTools, webConfiguration)
        {
            HasManagePermission = new JurisdictionManageFeature().HasPermissionByPerson(currentPerson);
            IsAdmin = currentPerson.IsAdministrator();
            IsManagerOrEditor = currentPerson.IsJurisdictionEditorOrManager();
            HasJurisdictionEditPermission = new JurisdictionEditFeature().HasPermissionByPerson(currentPerson);
            HasNeptuneViewAndRequiresJurisdictionsPermission =
                new NeptuneViewAndRequiresJurisdictionsFeature().HasPermissionByPerson(currentPerson);
            EntityName = "Data Hub";
            PageTitle = "Index";
            TreatmentBMPPage = new ViewPageContentViewData(linkGenerator, treatmentBMPPage, currentPerson);
            DelineationPage = new ViewPageContentViewData(linkGenerator, delineationPage, currentPerson);
            FieldTripPage = new ViewPageContentViewData(linkGenerator, fieldTripPage, currentPerson);
            WQMPPage = new ViewPageContentViewData(linkGenerator, wqmpPage, currentPerson);
            SimplifiedBMPPage = new ViewPageContentViewData(linkGenerator, simplifiedBMPPage, currentPerson);
            WQMPLocationPage = new ViewPageContentViewData(linkGenerator, wqmpLocationPage, currentPerson);
            AssessmentAreaPage = new ViewPageContentViewData(linkGenerator, assessmentAreaPage, currentPerson);
            OVTAsPage = new ViewPageContentViewData(linkGenerator, ovtasPage, currentPerson);
            LandUseBlocksPage = new ViewPageContentViewData(linkGenerator, landUseBlocksPage, currentPerson);
            ParcelPage = new ViewPageContentViewData(linkGenerator, parcelPage, currentPerson);
            RegionalSubbasinsPage = new ViewPageContentViewData(linkGenerator, regionalSubbasinsPage, currentPerson);
            LandUseStatisticsPage = new ViewPageContentViewData(linkGenerator, landUseStatisticsPage, currentPerson);
            ModelBasinsPage = new ViewPageContentViewData(linkGenerator, modelBasinsPage, currentPerson);
            PrecipitationZonesPage = new ViewPageContentViewData(linkGenerator, precipitationZonesPage, currentPerson);
            UploadTreatmentBMPUrl = SitkaRoute<TreatmentBMPController>.BuildUrlFromExpression(linkGenerator, x => x.UploadBMPs());
            DownloadTreatmentBMPUrl = SitkaRoute<TreatmentBMPController>.BuildUrlFromExpression(linkGenerator, x => x.DownloadBMPsToGIS());
            UploadDelineationUrl = SitkaRoute<DelineationGeometryController>.BuildUrlFromExpression(linkGenerator, x => x.UpdateDelineationGeometry());
            DownloadDelineationUrl = SitkaRoute<DelineationGeometryController>.BuildUrlFromExpression(linkGenerator, x => x.DownloadDelineationGeometry());
            UploadFieldTripUrl = SitkaRoute<FieldVisitController>.BuildUrlFromExpression(linkGenerator, x => x.BulkUploadTrashScreenVisit());
            UploadWQMPUrl = SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(linkGenerator, x => x.UploadWqmps());
            UploadSimplifiedBMPUrl = SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(linkGenerator, x => x.UploadSimplifiedBMPs());
            UploadWQMPLocationsUrl = SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(linkGenerator, x => x.UploadWqmpBoundaryFromAPNs());
            UploadAssessmentAreasUrl = SitkaRoute<OnlandVisualTrashAssessmentAreaController>.BuildUrlFromExpression(linkGenerator, x => x.BulkUploadOVTAAreas());
            DownloadAssessmentAreasUrl = SitkaRoute<OnlandVisualTrashAssessmentExportController>.BuildUrlFromExpression(linkGenerator, x => x.ExportAssessmentGeospatialData());
            UploadOVTAUrl = SitkaRoute<OnlandVisualTrashAssessmentController>.BuildUrlFromExpression(linkGenerator, x => x.BulkUploadOTVAs());
            UploadLandUseBlocksUrl = SitkaRoute<LandUseBlockGeometryController>.BuildUrlFromExpression(linkGenerator, x => x.UpdateLandUseBlockGeometry());
            DownloadLandUseBlocksUrl = SitkaRoute<LandUseBlockGeometryController>.BuildUrlFromExpression(linkGenerator, x => x.DownloadLandUseBlockGeometry());
            ParcelRefreshUrl = SitkaRoute<ParcelController>.BuildUrlFromExpression(linkGenerator, x => x.RefreshParcelsFromOCSurvey());
            RegionalSubbasinRefreshUrl = SitkaRoute<RegionalSubbasinController>.BuildUrlFromExpression(LinkGenerator, x => x.RefreshFromOCSurvey());
            LandUseStatisticsRefreshUrl = SitkaRoute<HRUCharacteristicController>.BuildUrlFromExpression(LinkGenerator, x => x.RefreshHRUCharacteristics());
            ModelBasinsRefreshUrl = SitkaRoute<TreatmentBMPController>.BuildUrlFromExpression(LinkGenerator, x => x.RefreshModelBasinsFromOCSurvey());
            PrecipitationZonesRefreshUrl = SitkaRoute<TreatmentBMPController>.BuildUrlFromExpression(LinkGenerator, x => x.RefreshPrecipitationZonesFromOCSurvey());

            LastUpdatedParcels = lastUpdatedParcels;
            LastUpdatedRegionalSubbasins = lastUpdatedRegionalSubbasins;
            LastUpdatedHRUCharacteristics = lastUpdatedDateHRUCharacteristics;
            LastUpdatedModalBasins = lastUpdatedDateModelBasins;
            LastUpdatedPrecipitationZones = lastUpdatedDatePrecipitationZones;

            BMPListUrl = SitkaRoute<TreatmentBMPController>.BuildUrlFromExpression(linkGenerator, x => x.Index());
            FieldTripUrl = SitkaRoute<FieldVisitController>.BuildUrlFromExpression(linkGenerator, x => x.Index());
            ModelingAttributeUrl = SitkaRoute<TreatmentBMPController>.BuildUrlFromExpression(linkGenerator, x => x.ViewTreatmentBMPModelingAttributes());
            BMPMapUrl = SitkaRoute<TreatmentBMPController>.BuildUrlFromExpression(linkGenerator, x => x.FindABMP());
            BMPTypesUrl = SitkaRoute<TreatmentBMPTypeController>.BuildUrlFromExpression(linkGenerator, x => x.Index());
            
            WQMPListUrl = SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(linkGenerator, x => x.Index());

            OVTAListUrl = $"{webConfiguration.TrashModuleBaseUrl}/onland-visual-trash-assessments";
            TrashGeneratingUnitsAuditListUrl = $"{webConfiguration.TrashModuleBaseUrl}/trash-analysis-areas";
            LandUseBlocksUrl = $"{webConfiguration.TrashModuleBaseUrl}/land-use-blocks";
            
            ParcelMapUrl = SitkaRoute<ParcelController>.BuildUrlFromExpression(linkGenerator, x => x.Index());
            
            RSBListUrl = SitkaRoute<RegionalSubbasinController>.BuildUrlFromExpression(linkGenerator, x => x.Index());
            RSBGridUrl = SitkaRoute<RegionalSubbasinController>.BuildUrlFromExpression(linkGenerator, x => x.Grid());
            HRUCharacteristicsListUrl = SitkaRoute<HRUCharacteristicController>.BuildUrlFromExpression(linkGenerator, x => x.Index());
        }
    }
}