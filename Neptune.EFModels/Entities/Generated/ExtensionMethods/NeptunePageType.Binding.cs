//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[NeptunePageType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class NeptunePageType : IHavePrimaryKey
    {
        public static readonly NeptunePageTypeHomePage HomePage = NeptunePageTypeHomePage.Instance;
        public static readonly NeptunePageTypeAbout About = NeptunePageTypeAbout.Instance;
        public static readonly NeptunePageTypeOrganizationsList OrganizationsList = NeptunePageTypeOrganizationsList.Instance;
        public static readonly NeptunePageTypeHomeMapInfo HomeMapInfo = NeptunePageTypeHomeMapInfo.Instance;
        public static readonly NeptunePageTypeHomeAdditionalInfo HomeAdditionalInfo = NeptunePageTypeHomeAdditionalInfo.Instance;
        public static readonly NeptunePageTypeTreatmentBMP TreatmentBMP = NeptunePageTypeTreatmentBMP.Instance;
        public static readonly NeptunePageTypeTreatmentBMPType TreatmentBMPType = NeptunePageTypeTreatmentBMPType.Instance;
        public static readonly NeptunePageTypeJurisdiction Jurisdiction = NeptunePageTypeJurisdiction.Instance;
        public static readonly NeptunePageTypeAssessment Assessment = NeptunePageTypeAssessment.Instance;
        public static readonly NeptunePageTypeManageObservationTypesList ManageObservationTypesList = NeptunePageTypeManageObservationTypesList.Instance;
        public static readonly NeptunePageTypeManageTreatmentBMPTypesList ManageTreatmentBMPTypesList = NeptunePageTypeManageTreatmentBMPTypesList.Instance;
        public static readonly NeptunePageTypeManageObservationTypeInstructions ManageObservationTypeInstructions = NeptunePageTypeManageObservationTypeInstructions.Instance;
        public static readonly NeptunePageTypeManageObservationTypeObservationInstructions ManageObservationTypeObservationInstructions = NeptunePageTypeManageObservationTypeObservationInstructions.Instance;
        public static readonly NeptunePageTypeManageObservationTypeLabelsAndUnitsInstructions ManageObservationTypeLabelsAndUnitsInstructions = NeptunePageTypeManageObservationTypeLabelsAndUnitsInstructions.Instance;
        public static readonly NeptunePageTypeManageTreatmentBMPTypeInstructions ManageTreatmentBMPTypeInstructions = NeptunePageTypeManageTreatmentBMPTypeInstructions.Instance;
        public static readonly NeptunePageTypeManageCustomAttributeTypeInstructions ManageCustomAttributeTypeInstructions = NeptunePageTypeManageCustomAttributeTypeInstructions.Instance;
        public static readonly NeptunePageTypeManageCustomAttributeInstructions ManageCustomAttributeInstructions = NeptunePageTypeManageCustomAttributeInstructions.Instance;
        public static readonly NeptunePageTypeManageCustomAttributeTypesList ManageCustomAttributeTypesList = NeptunePageTypeManageCustomAttributeTypesList.Instance;
        public static readonly NeptunePageTypeLegal Legal = NeptunePageTypeLegal.Instance;
        public static readonly NeptunePageTypeFundingSourcesList FundingSourcesList = NeptunePageTypeFundingSourcesList.Instance;
        public static readonly NeptunePageTypeFindABMP FindABMP = NeptunePageTypeFindABMP.Instance;
        public static readonly NeptunePageTypeLaunchPad LaunchPad = NeptunePageTypeLaunchPad.Instance;
        public static readonly NeptunePageTypeFieldRecords FieldRecords = NeptunePageTypeFieldRecords.Instance;
        public static readonly NeptunePageTypeRequestSupport RequestSupport = NeptunePageTypeRequestSupport.Instance;
        public static readonly NeptunePageTypeInviteUser InviteUser = NeptunePageTypeInviteUser.Instance;
        public static readonly NeptunePageTypeWaterQualityMaintenancePlan WaterQualityMaintenancePlan = NeptunePageTypeWaterQualityMaintenancePlan.Instance;
        public static readonly NeptunePageTypeParcelList ParcelList = NeptunePageTypeParcelList.Instance;
        public static readonly NeptunePageTypeTraining Training = NeptunePageTypeTraining.Instance;
        public static readonly NeptunePageTypeManagerDashboard ManagerDashboard = NeptunePageTypeManagerDashboard.Instance;
        public static readonly NeptunePageTypeWaterQualityMaintenancePlanOandMVerifications WaterQualityMaintenancePlanOandMVerifications = NeptunePageTypeWaterQualityMaintenancePlanOandMVerifications.Instance;
        public static readonly NeptunePageTypeModelingHomePage ModelingHomePage = NeptunePageTypeModelingHomePage.Instance;
        public static readonly NeptunePageTypeTrashHomePage TrashHomePage = NeptunePageTypeTrashHomePage.Instance;
        public static readonly NeptunePageTypeOVTAInstructions OVTAInstructions = NeptunePageTypeOVTAInstructions.Instance;
        public static readonly NeptunePageTypeOVTAIndex OVTAIndex = NeptunePageTypeOVTAIndex.Instance;
        public static readonly NeptunePageTypeTrashModuleProgramOverview TrashModuleProgramOverview = NeptunePageTypeTrashModuleProgramOverview.Instance;
        public static readonly NeptunePageTypeDelineationMap DelineationMap = NeptunePageTypeDelineationMap.Instance;
        public static readonly NeptunePageTypeBulkUploadRequest BulkUploadRequest = NeptunePageTypeBulkUploadRequest.Instance;
        public static readonly NeptunePageTypeTreatmentBMPAssessment TreatmentBMPAssessment = NeptunePageTypeTreatmentBMPAssessment.Instance;
        public static readonly NeptunePageTypeEditOVTAArea EditOVTAArea = NeptunePageTypeEditOVTAArea.Instance;
        public static readonly NeptunePageTypeLandUseBlock LandUseBlock = NeptunePageTypeLandUseBlock.Instance;
        public static readonly NeptunePageTypeExportAssessmentGeospatialData ExportAssessmentGeospatialData = NeptunePageTypeExportAssessmentGeospatialData.Instance;
        public static readonly NeptunePageTypeHRUCharacteristics HRUCharacteristics = NeptunePageTypeHRUCharacteristics.Instance;
        public static readonly NeptunePageTypeRegionalSubbasins RegionalSubbasins = NeptunePageTypeRegionalSubbasins.Instance;
        public static readonly NeptunePageTypeDelineationReconciliationReport DelineationReconciliationReport = NeptunePageTypeDelineationReconciliationReport.Instance;
        public static readonly NeptunePageTypeViewTreatmentBMPModelingAttributes ViewTreatmentBMPModelingAttributes = NeptunePageTypeViewTreatmentBMPModelingAttributes.Instance;
        public static readonly NeptunePageTypeUploadTreatmentBMPs UploadTreatmentBMPs = NeptunePageTypeUploadTreatmentBMPs.Instance;
        public static readonly NeptunePageTypeAboutModelingBMPPerformance AboutModelingBMPPerformance = NeptunePageTypeAboutModelingBMPPerformance.Instance;
        public static readonly NeptunePageTypeBulkUploadFieldVisits BulkUploadFieldVisits = NeptunePageTypeBulkUploadFieldVisits.Instance;
        public static readonly NeptunePageTypeHippocampHomePage HippocampHomePage = NeptunePageTypeHippocampHomePage.Instance;
        public static readonly NeptunePageTypeHippocampTraining HippocampTraining = NeptunePageTypeHippocampTraining.Instance;
        public static readonly NeptunePageTypeHippocampLabelsAndDefinitionsList HippocampLabelsAndDefinitionsList = NeptunePageTypeHippocampLabelsAndDefinitionsList.Instance;
        public static readonly NeptunePageTypeHippocampAbout HippocampAbout = NeptunePageTypeHippocampAbout.Instance;
        public static readonly NeptunePageTypeHippocampProjectsList HippocampProjectsList = NeptunePageTypeHippocampProjectsList.Instance;
        public static readonly NeptunePageTypeHippocampProjectInstructions HippocampProjectInstructions = NeptunePageTypeHippocampProjectInstructions.Instance;
        public static readonly NeptunePageTypeHippocampProjectBasics HippocampProjectBasics = NeptunePageTypeHippocampProjectBasics.Instance;
        public static readonly NeptunePageTypeHippocampProjectAttachments HippocampProjectAttachments = NeptunePageTypeHippocampProjectAttachments.Instance;
        public static readonly NeptunePageTypeHippocampTreatmentBMPs HippocampTreatmentBMPs = NeptunePageTypeHippocampTreatmentBMPs.Instance;
        public static readonly NeptunePageTypeHippocampDelineations HippocampDelineations = NeptunePageTypeHippocampDelineations.Instance;
        public static readonly NeptunePageTypeHippocampModeledPerformance HippocampModeledPerformance = NeptunePageTypeHippocampModeledPerformance.Instance;
        public static readonly NeptunePageTypeHippocampReview HippocampReview = NeptunePageTypeHippocampReview.Instance;
        public static readonly NeptunePageTypeHippocampPlanningMap HippocampPlanningMap = NeptunePageTypeHippocampPlanningMap.Instance;
        public static readonly NeptunePageTypeOCTAM2Tier2GrantProgramMetrics OCTAM2Tier2GrantProgramMetrics = NeptunePageTypeOCTAM2Tier2GrantProgramMetrics.Instance;
        public static readonly NeptunePageTypeOCTAM2Tier2GrantProgramDashboard OCTAM2Tier2GrantProgramDashboard = NeptunePageTypeOCTAM2Tier2GrantProgramDashboard.Instance;
        public static readonly NeptunePageTypeEditWQMPBoundary EditWQMPBoundary = NeptunePageTypeEditWQMPBoundary.Instance;
        public static readonly NeptunePageTypeUploadWQMPs UploadWQMPs = NeptunePageTypeUploadWQMPs.Instance;
        public static readonly NeptunePageTypeUploadSimplifiedBMPs UploadSimplifiedBMPs = NeptunePageTypeUploadSimplifiedBMPs.Instance;
        public static readonly NeptunePageTypeUploadOVTAs UploadOVTAs = NeptunePageTypeUploadOVTAs.Instance;
        public static readonly NeptunePageTypeWQMPBoundaryFromAPNList WQMPBoundaryFromAPNList = NeptunePageTypeWQMPBoundaryFromAPNList.Instance;
        public static readonly NeptunePageTypeBMPDataHub BMPDataHub = NeptunePageTypeBMPDataHub.Instance;
        public static readonly NeptunePageTypeDelineationDataHub DelineationDataHub = NeptunePageTypeDelineationDataHub.Instance;
        public static readonly NeptunePageTypeFieldVisitDataHub FieldVisitDataHub = NeptunePageTypeFieldVisitDataHub.Instance;
        public static readonly NeptunePageTypeWQMPDataHub WQMPDataHub = NeptunePageTypeWQMPDataHub.Instance;
        public static readonly NeptunePageTypeSimplifiedBMPsDataHub SimplifiedBMPsDataHub = NeptunePageTypeSimplifiedBMPsDataHub.Instance;
        public static readonly NeptunePageTypeWQMPLocationsDataHub WQMPLocationsDataHub = NeptunePageTypeWQMPLocationsDataHub.Instance;
        public static readonly NeptunePageTypeAssessmentAreasDataHub AssessmentAreasDataHub = NeptunePageTypeAssessmentAreasDataHub.Instance;
        public static readonly NeptunePageTypeOVTADataHub OVTADataHub = NeptunePageTypeOVTADataHub.Instance;
        public static readonly NeptunePageTypeLandUseBlockDataHub LandUseBlockDataHub = NeptunePageTypeLandUseBlockDataHub.Instance;
        public static readonly NeptunePageTypeRegionalSubbasinsDataHub RegionalSubbasinsDataHub = NeptunePageTypeRegionalSubbasinsDataHub.Instance;
        public static readonly NeptunePageTypeLandUseStatisticsDataHub LandUseStatisticsDataHub = NeptunePageTypeLandUseStatisticsDataHub.Instance;
        public static readonly NeptunePageTypeModelBasinsDataHub ModelBasinsDataHub = NeptunePageTypeModelBasinsDataHub.Instance;
        public static readonly NeptunePageTypePrecipitationZonesDataHub PrecipitationZonesDataHub = NeptunePageTypePrecipitationZonesDataHub.Instance;
        public static readonly NeptunePageTypeParcelUploadDataHub ParcelUploadDataHub = NeptunePageTypeParcelUploadDataHub.Instance;
        public static readonly NeptunePageTypeExportBMPInventoryToGIS ExportBMPInventoryToGIS = NeptunePageTypeExportBMPInventoryToGIS.Instance;
        public static readonly NeptunePageTypeSPAHomePage SPAHomePage = NeptunePageTypeSPAHomePage.Instance;
        public static readonly NeptunePageTypeWQMPModelingOptions WQMPModelingOptions = NeptunePageTypeWQMPModelingOptions.Instance;
        public static readonly NeptunePageTypeWQMPApprovalSummary WQMPApprovalSummary = NeptunePageTypeWQMPApprovalSummary.Instance;
        public static readonly NeptunePageTypeWQMPPostConstructionInspectionAndVerification WQMPPostConstructionInspectionAndVerification = NeptunePageTypeWQMPPostConstructionInspectionAndVerification.Instance;
        public static readonly NeptunePageTypeWQMPMap WQMPMap = NeptunePageTypeWQMPMap.Instance;
        public static readonly NeptunePageTypeWQMPVerificationBasics WQMPVerificationBasics = NeptunePageTypeWQMPVerificationBasics.Instance;
        public static readonly NeptunePageTypeWQMPVerificationStructuralBmps WQMPVerificationStructuralBmps = NeptunePageTypeWQMPVerificationStructuralBmps.Instance;
        public static readonly NeptunePageTypeWQMPVerificationSimplifiedBmps WQMPVerificationSimplifiedBmps = NeptunePageTypeWQMPVerificationSimplifiedBmps.Instance;
        public static readonly NeptunePageTypeWQMPVerificationSourceControl WQMPVerificationSourceControl = NeptunePageTypeWQMPVerificationSourceControl.Instance;
        public static readonly NeptunePageTypeWQMPVerificationReview WQMPVerificationReview = NeptunePageTypeWQMPVerificationReview.Instance;
        public static readonly NeptunePageTypeWQMPVerificationSupportingDocumentation WQMPVerificationSupportingDocumentation = NeptunePageTypeWQMPVerificationSupportingDocumentation.Instance;
        public static readonly NeptunePageTypeWebServices WebServices = NeptunePageTypeWebServices.Instance;
        public static readonly NeptunePageTypeOCTAPrioritizationDataHub OCTAPrioritizationDataHub = NeptunePageTypeOCTAPrioritizationDataHub.Instance;

        public static readonly List<NeptunePageType> All;
        public static readonly ReadOnlyDictionary<int, NeptunePageType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static NeptunePageType()
        {
            All = new List<NeptunePageType> { HomePage, About, OrganizationsList, HomeMapInfo, HomeAdditionalInfo, TreatmentBMP, TreatmentBMPType, Jurisdiction, Assessment, ManageObservationTypesList, ManageTreatmentBMPTypesList, ManageObservationTypeInstructions, ManageObservationTypeObservationInstructions, ManageObservationTypeLabelsAndUnitsInstructions, ManageTreatmentBMPTypeInstructions, ManageCustomAttributeTypeInstructions, ManageCustomAttributeInstructions, ManageCustomAttributeTypesList, Legal, FundingSourcesList, FindABMP, LaunchPad, FieldRecords, RequestSupport, InviteUser, WaterQualityMaintenancePlan, ParcelList, Training, ManagerDashboard, WaterQualityMaintenancePlanOandMVerifications, ModelingHomePage, TrashHomePage, OVTAInstructions, OVTAIndex, TrashModuleProgramOverview, DelineationMap, BulkUploadRequest, TreatmentBMPAssessment, EditOVTAArea, LandUseBlock, ExportAssessmentGeospatialData, HRUCharacteristics, RegionalSubbasins, DelineationReconciliationReport, ViewTreatmentBMPModelingAttributes, UploadTreatmentBMPs, AboutModelingBMPPerformance, BulkUploadFieldVisits, HippocampHomePage, HippocampTraining, HippocampLabelsAndDefinitionsList, HippocampAbout, HippocampProjectsList, HippocampProjectInstructions, HippocampProjectBasics, HippocampProjectAttachments, HippocampTreatmentBMPs, HippocampDelineations, HippocampModeledPerformance, HippocampReview, HippocampPlanningMap, OCTAM2Tier2GrantProgramMetrics, OCTAM2Tier2GrantProgramDashboard, EditWQMPBoundary, UploadWQMPs, UploadSimplifiedBMPs, UploadOVTAs, WQMPBoundaryFromAPNList, BMPDataHub, DelineationDataHub, FieldVisitDataHub, WQMPDataHub, SimplifiedBMPsDataHub, WQMPLocationsDataHub, AssessmentAreasDataHub, OVTADataHub, LandUseBlockDataHub, RegionalSubbasinsDataHub, LandUseStatisticsDataHub, ModelBasinsDataHub, PrecipitationZonesDataHub, ParcelUploadDataHub, ExportBMPInventoryToGIS, SPAHomePage, WQMPModelingOptions, WQMPApprovalSummary, WQMPPostConstructionInspectionAndVerification, WQMPMap, WQMPVerificationBasics, WQMPVerificationStructuralBmps, WQMPVerificationSimplifiedBmps, WQMPVerificationSourceControl, WQMPVerificationReview, WQMPVerificationSupportingDocumentation, WebServices, OCTAPrioritizationDataHub };
            AllLookupDictionary = new ReadOnlyDictionary<int, NeptunePageType>(All.ToDictionary(x => x.NeptunePageTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected NeptunePageType(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName)
        {
            NeptunePageTypeID = neptunePageTypeID;
            NeptunePageTypeName = neptunePageTypeName;
            NeptunePageTypeDisplayName = neptunePageTypeDisplayName;
        }

        [Key]
        public int NeptunePageTypeID { get; private set; }
        public string NeptunePageTypeName { get; private set; }
        public string NeptunePageTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return NeptunePageTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(NeptunePageType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.NeptunePageTypeID == NeptunePageTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as NeptunePageType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return NeptunePageTypeID;
        }

        public static bool operator ==(NeptunePageType left, NeptunePageType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(NeptunePageType left, NeptunePageType right)
        {
            return !Equals(left, right);
        }

        public NeptunePageTypeEnum ToEnum => (NeptunePageTypeEnum)GetHashCode();
    }

    public enum NeptunePageTypeEnum
    {
        HomePage = 1,
        About = 2,
        OrganizationsList = 3,
        HomeMapInfo = 4,
        HomeAdditionalInfo = 5,
        TreatmentBMP = 6,
        TreatmentBMPType = 7,
        Jurisdiction = 9,
        Assessment = 10,
        ManageObservationTypesList = 11,
        ManageTreatmentBMPTypesList = 12,
        ManageObservationTypeInstructions = 13,
        ManageObservationTypeObservationInstructions = 14,
        ManageObservationTypeLabelsAndUnitsInstructions = 15,
        ManageTreatmentBMPTypeInstructions = 16,
        ManageCustomAttributeTypeInstructions = 17,
        ManageCustomAttributeInstructions = 18,
        ManageCustomAttributeTypesList = 19,
        Legal = 20,
        FundingSourcesList = 21,
        FindABMP = 22,
        LaunchPad = 23,
        FieldRecords = 24,
        RequestSupport = 25,
        InviteUser = 26,
        WaterQualityMaintenancePlan = 27,
        ParcelList = 28,
        Training = 29,
        ManagerDashboard = 30,
        WaterQualityMaintenancePlanOandMVerifications = 31,
        ModelingHomePage = 32,
        TrashHomePage = 33,
        OVTAInstructions = 34,
        OVTAIndex = 35,
        TrashModuleProgramOverview = 36,
        DelineationMap = 37,
        BulkUploadRequest = 38,
        TreatmentBMPAssessment = 41,
        EditOVTAArea = 42,
        LandUseBlock = 43,
        ExportAssessmentGeospatialData = 44,
        HRUCharacteristics = 45,
        RegionalSubbasins = 46,
        DelineationReconciliationReport = 47,
        ViewTreatmentBMPModelingAttributes = 48,
        UploadTreatmentBMPs = 49,
        AboutModelingBMPPerformance = 50,
        BulkUploadFieldVisits = 51,
        HippocampHomePage = 52,
        HippocampTraining = 53,
        HippocampLabelsAndDefinitionsList = 54,
        HippocampAbout = 55,
        HippocampProjectsList = 56,
        HippocampProjectInstructions = 57,
        HippocampProjectBasics = 58,
        HippocampProjectAttachments = 59,
        HippocampTreatmentBMPs = 60,
        HippocampDelineations = 61,
        HippocampModeledPerformance = 62,
        HippocampReview = 63,
        HippocampPlanningMap = 64,
        OCTAM2Tier2GrantProgramMetrics = 65,
        OCTAM2Tier2GrantProgramDashboard = 66,
        EditWQMPBoundary = 67,
        UploadWQMPs = 68,
        UploadSimplifiedBMPs = 69,
        UploadOVTAs = 70,
        WQMPBoundaryFromAPNList = 71,
        BMPDataHub = 72,
        DelineationDataHub = 73,
        FieldVisitDataHub = 74,
        WQMPDataHub = 75,
        SimplifiedBMPsDataHub = 76,
        WQMPLocationsDataHub = 77,
        AssessmentAreasDataHub = 78,
        OVTADataHub = 79,
        LandUseBlockDataHub = 80,
        RegionalSubbasinsDataHub = 81,
        LandUseStatisticsDataHub = 82,
        ModelBasinsDataHub = 83,
        PrecipitationZonesDataHub = 84,
        ParcelUploadDataHub = 85,
        ExportBMPInventoryToGIS = 86,
        SPAHomePage = 87,
        WQMPModelingOptions = 88,
        WQMPApprovalSummary = 89,
        WQMPPostConstructionInspectionAndVerification = 90,
        WQMPMap = 91,
        WQMPVerificationBasics = 93,
        WQMPVerificationStructuralBmps = 94,
        WQMPVerificationSimplifiedBmps = 95,
        WQMPVerificationSourceControl = 96,
        WQMPVerificationReview = 97,
        WQMPVerificationSupportingDocumentation = 98,
        WebServices = 99,
        OCTAPrioritizationDataHub = 100
    }

    public partial class NeptunePageTypeHomePage : NeptunePageType
    {
        private NeptunePageTypeHomePage(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHomePage Instance = new NeptunePageTypeHomePage(1, @"HomePage", @"Home Page");
    }

    public partial class NeptunePageTypeAbout : NeptunePageType
    {
        private NeptunePageTypeAbout(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeAbout Instance = new NeptunePageTypeAbout(2, @"About", @"About");
    }

    public partial class NeptunePageTypeOrganizationsList : NeptunePageType
    {
        private NeptunePageTypeOrganizationsList(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeOrganizationsList Instance = new NeptunePageTypeOrganizationsList(3, @"OrganizationsList", @"Organizations List");
    }

    public partial class NeptunePageTypeHomeMapInfo : NeptunePageType
    {
        private NeptunePageTypeHomeMapInfo(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHomeMapInfo Instance = new NeptunePageTypeHomeMapInfo(4, @"HomeMapInfo", @"Home Page Map Info");
    }

    public partial class NeptunePageTypeHomeAdditionalInfo : NeptunePageType
    {
        private NeptunePageTypeHomeAdditionalInfo(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHomeAdditionalInfo Instance = new NeptunePageTypeHomeAdditionalInfo(5, @"HomeAdditionalInfo", @"Home Page Additional Info");
    }

    public partial class NeptunePageTypeTreatmentBMP : NeptunePageType
    {
        private NeptunePageTypeTreatmentBMP(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeTreatmentBMP Instance = new NeptunePageTypeTreatmentBMP(6, @"TreatmentBMP", @"Treatment BMP");
    }

    public partial class NeptunePageTypeTreatmentBMPType : NeptunePageType
    {
        private NeptunePageTypeTreatmentBMPType(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeTreatmentBMPType Instance = new NeptunePageTypeTreatmentBMPType(7, @"TreatmentBMPType", @"Treatment BMP Type");
    }

    public partial class NeptunePageTypeJurisdiction : NeptunePageType
    {
        private NeptunePageTypeJurisdiction(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeJurisdiction Instance = new NeptunePageTypeJurisdiction(9, @"Jurisdiction", @"Jurisdiction");
    }

    public partial class NeptunePageTypeAssessment : NeptunePageType
    {
        private NeptunePageTypeAssessment(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeAssessment Instance = new NeptunePageTypeAssessment(10, @"Assessment", @"Assessment");
    }

    public partial class NeptunePageTypeManageObservationTypesList : NeptunePageType
    {
        private NeptunePageTypeManageObservationTypesList(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeManageObservationTypesList Instance = new NeptunePageTypeManageObservationTypesList(11, @"ManageObservationTypesList", @"Manage Observation Types List");
    }

    public partial class NeptunePageTypeManageTreatmentBMPTypesList : NeptunePageType
    {
        private NeptunePageTypeManageTreatmentBMPTypesList(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeManageTreatmentBMPTypesList Instance = new NeptunePageTypeManageTreatmentBMPTypesList(12, @"ManageTreatmentBMPTypesList", @"Manage Treatment BMP Types List");
    }

    public partial class NeptunePageTypeManageObservationTypeInstructions : NeptunePageType
    {
        private NeptunePageTypeManageObservationTypeInstructions(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeManageObservationTypeInstructions Instance = new NeptunePageTypeManageObservationTypeInstructions(13, @"ManageObservationTypeInstructions", @"Manage Observation Type Instructions");
    }

    public partial class NeptunePageTypeManageObservationTypeObservationInstructions : NeptunePageType
    {
        private NeptunePageTypeManageObservationTypeObservationInstructions(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeManageObservationTypeObservationInstructions Instance = new NeptunePageTypeManageObservationTypeObservationInstructions(14, @"ManageObservationTypeObservationInstructions", @"Manage Observation Type Instructions for Observation Instructions");
    }

    public partial class NeptunePageTypeManageObservationTypeLabelsAndUnitsInstructions : NeptunePageType
    {
        private NeptunePageTypeManageObservationTypeLabelsAndUnitsInstructions(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeManageObservationTypeLabelsAndUnitsInstructions Instance = new NeptunePageTypeManageObservationTypeLabelsAndUnitsInstructions(15, @"ManageObservationTypeLabelsAndUnitsInstructions", @"Manage Observation Type Labels and Units Instructions");
    }

    public partial class NeptunePageTypeManageTreatmentBMPTypeInstructions : NeptunePageType
    {
        private NeptunePageTypeManageTreatmentBMPTypeInstructions(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeManageTreatmentBMPTypeInstructions Instance = new NeptunePageTypeManageTreatmentBMPTypeInstructions(16, @"ManageTreatmentBMPTypeInstructions", @"Manage Treatment BMP Type Instructions");
    }

    public partial class NeptunePageTypeManageCustomAttributeTypeInstructions : NeptunePageType
    {
        private NeptunePageTypeManageCustomAttributeTypeInstructions(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeManageCustomAttributeTypeInstructions Instance = new NeptunePageTypeManageCustomAttributeTypeInstructions(17, @"ManageCustomAttributeTypeInstructions", @"Manage Custom Attribute Type Instructions");
    }

    public partial class NeptunePageTypeManageCustomAttributeInstructions : NeptunePageType
    {
        private NeptunePageTypeManageCustomAttributeInstructions(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeManageCustomAttributeInstructions Instance = new NeptunePageTypeManageCustomAttributeInstructions(18, @"ManageCustomAttributeInstructions", @"Manage Custom Attribute Instructions");
    }

    public partial class NeptunePageTypeManageCustomAttributeTypesList : NeptunePageType
    {
        private NeptunePageTypeManageCustomAttributeTypesList(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeManageCustomAttributeTypesList Instance = new NeptunePageTypeManageCustomAttributeTypesList(19, @"ManageCustomAttributeTypesList", @"Manage Custom Attribute Types List");
    }

    public partial class NeptunePageTypeLegal : NeptunePageType
    {
        private NeptunePageTypeLegal(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeLegal Instance = new NeptunePageTypeLegal(20, @"Legal", @"Legal");
    }

    public partial class NeptunePageTypeFundingSourcesList : NeptunePageType
    {
        private NeptunePageTypeFundingSourcesList(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeFundingSourcesList Instance = new NeptunePageTypeFundingSourcesList(21, @"FundingSourcesList", @"Funding Sources List");
    }

    public partial class NeptunePageTypeFindABMP : NeptunePageType
    {
        private NeptunePageTypeFindABMP(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeFindABMP Instance = new NeptunePageTypeFindABMP(22, @"FindABMP", @"Find a BMP");
    }

    public partial class NeptunePageTypeLaunchPad : NeptunePageType
    {
        private NeptunePageTypeLaunchPad(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeLaunchPad Instance = new NeptunePageTypeLaunchPad(23, @"LaunchPad", @"Launch Pad");
    }

    public partial class NeptunePageTypeFieldRecords : NeptunePageType
    {
        private NeptunePageTypeFieldRecords(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeFieldRecords Instance = new NeptunePageTypeFieldRecords(24, @"FieldRecords", @"Field Records");
    }

    public partial class NeptunePageTypeRequestSupport : NeptunePageType
    {
        private NeptunePageTypeRequestSupport(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeRequestSupport Instance = new NeptunePageTypeRequestSupport(25, @"RequestSupport", @"Request Support");
    }

    public partial class NeptunePageTypeInviteUser : NeptunePageType
    {
        private NeptunePageTypeInviteUser(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeInviteUser Instance = new NeptunePageTypeInviteUser(26, @"InviteUser", @"Invite User");
    }

    public partial class NeptunePageTypeWaterQualityMaintenancePlan : NeptunePageType
    {
        private NeptunePageTypeWaterQualityMaintenancePlan(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWaterQualityMaintenancePlan Instance = new NeptunePageTypeWaterQualityMaintenancePlan(27, @"WaterQualityMaintenancePlan", @"Water Quality Maintenance Plan");
    }

    public partial class NeptunePageTypeParcelList : NeptunePageType
    {
        private NeptunePageTypeParcelList(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeParcelList Instance = new NeptunePageTypeParcelList(28, @"ParcelList", @"Parcel List");
    }

    public partial class NeptunePageTypeTraining : NeptunePageType
    {
        private NeptunePageTypeTraining(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeTraining Instance = new NeptunePageTypeTraining(29, @"Training", @"Training");
    }

    public partial class NeptunePageTypeManagerDashboard : NeptunePageType
    {
        private NeptunePageTypeManagerDashboard(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeManagerDashboard Instance = new NeptunePageTypeManagerDashboard(30, @"ManagerDashboard", @"Manager Dashboard");
    }

    public partial class NeptunePageTypeWaterQualityMaintenancePlanOandMVerifications : NeptunePageType
    {
        private NeptunePageTypeWaterQualityMaintenancePlanOandMVerifications(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWaterQualityMaintenancePlanOandMVerifications Instance = new NeptunePageTypeWaterQualityMaintenancePlanOandMVerifications(31, @"WaterQualityMaintenancePlanOandMVerifications", @"Water Quality Maintenance Plan O&M Verifications");
    }

    public partial class NeptunePageTypeModelingHomePage : NeptunePageType
    {
        private NeptunePageTypeModelingHomePage(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeModelingHomePage Instance = new NeptunePageTypeModelingHomePage(32, @"ModelingHomePage", @"Modeling Module");
    }

    public partial class NeptunePageTypeTrashHomePage : NeptunePageType
    {
        private NeptunePageTypeTrashHomePage(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeTrashHomePage Instance = new NeptunePageTypeTrashHomePage(33, @"TrashHomePage", @"Trash Module Home Page");
    }

    public partial class NeptunePageTypeOVTAInstructions : NeptunePageType
    {
        private NeptunePageTypeOVTAInstructions(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeOVTAInstructions Instance = new NeptunePageTypeOVTAInstructions(34, @"OVTAInstructions", @"OVTA Instructions");
    }

    public partial class NeptunePageTypeOVTAIndex : NeptunePageType
    {
        private NeptunePageTypeOVTAIndex(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeOVTAIndex Instance = new NeptunePageTypeOVTAIndex(35, @"OVTAIndex", @"OVTA Index");
    }

    public partial class NeptunePageTypeTrashModuleProgramOverview : NeptunePageType
    {
        private NeptunePageTypeTrashModuleProgramOverview(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeTrashModuleProgramOverview Instance = new NeptunePageTypeTrashModuleProgramOverview(36, @"TrashModuleProgramOverview", @"Trash Module Program Overview");
    }

    public partial class NeptunePageTypeDelineationMap : NeptunePageType
    {
        private NeptunePageTypeDelineationMap(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeDelineationMap Instance = new NeptunePageTypeDelineationMap(37, @"DelineationMap", @"Delineation Map");
    }

    public partial class NeptunePageTypeBulkUploadRequest : NeptunePageType
    {
        private NeptunePageTypeBulkUploadRequest(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeBulkUploadRequest Instance = new NeptunePageTypeBulkUploadRequest(38, @"BulkUploadRequest", @"Bulk Upload Request");
    }

    public partial class NeptunePageTypeTreatmentBMPAssessment : NeptunePageType
    {
        private NeptunePageTypeTreatmentBMPAssessment(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeTreatmentBMPAssessment Instance = new NeptunePageTypeTreatmentBMPAssessment(41, @"TreatmentBMPAssessment", @"Treatment BMP Assessment");
    }

    public partial class NeptunePageTypeEditOVTAArea : NeptunePageType
    {
        private NeptunePageTypeEditOVTAArea(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeEditOVTAArea Instance = new NeptunePageTypeEditOVTAArea(42, @"EditOVTAArea", @"Edit OVTA Area");
    }

    public partial class NeptunePageTypeLandUseBlock : NeptunePageType
    {
        private NeptunePageTypeLandUseBlock(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeLandUseBlock Instance = new NeptunePageTypeLandUseBlock(43, @"LandUseBlock", @"Land Use Block");
    }

    public partial class NeptunePageTypeExportAssessmentGeospatialData : NeptunePageType
    {
        private NeptunePageTypeExportAssessmentGeospatialData(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeExportAssessmentGeospatialData Instance = new NeptunePageTypeExportAssessmentGeospatialData(44, @"ExportAssessmentGeospatialData", @"Export Assessment Geospatial Data");
    }

    public partial class NeptunePageTypeHRUCharacteristics : NeptunePageType
    {
        private NeptunePageTypeHRUCharacteristics(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHRUCharacteristics Instance = new NeptunePageTypeHRUCharacteristics(45, @"HRUCharacteristics", @"HRU Characteristics");
    }

    public partial class NeptunePageTypeRegionalSubbasins : NeptunePageType
    {
        private NeptunePageTypeRegionalSubbasins(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeRegionalSubbasins Instance = new NeptunePageTypeRegionalSubbasins(46, @"RegionalSubbasins", @"Regional Subbasins");
    }

    public partial class NeptunePageTypeDelineationReconciliationReport : NeptunePageType
    {
        private NeptunePageTypeDelineationReconciliationReport(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeDelineationReconciliationReport Instance = new NeptunePageTypeDelineationReconciliationReport(47, @"DelineationReconciliationReport", @"Delineation Reconciliation Report");
    }

    public partial class NeptunePageTypeViewTreatmentBMPModelingAttributes : NeptunePageType
    {
        private NeptunePageTypeViewTreatmentBMPModelingAttributes(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeViewTreatmentBMPModelingAttributes Instance = new NeptunePageTypeViewTreatmentBMPModelingAttributes(48, @"ViewTreatmentBMPModelingAttributes", @"View Treatment BMP Modeling Attributes");
    }

    public partial class NeptunePageTypeUploadTreatmentBMPs : NeptunePageType
    {
        private NeptunePageTypeUploadTreatmentBMPs(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeUploadTreatmentBMPs Instance = new NeptunePageTypeUploadTreatmentBMPs(49, @"UploadTreatmentBMPs", @"Upload Treatment BMPs");
    }

    public partial class NeptunePageTypeAboutModelingBMPPerformance : NeptunePageType
    {
        private NeptunePageTypeAboutModelingBMPPerformance(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeAboutModelingBMPPerformance Instance = new NeptunePageTypeAboutModelingBMPPerformance(50, @"AboutModelingBMPPerformance", @"About Modeling BMP Performance");
    }

    public partial class NeptunePageTypeBulkUploadFieldVisits : NeptunePageType
    {
        private NeptunePageTypeBulkUploadFieldVisits(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeBulkUploadFieldVisits Instance = new NeptunePageTypeBulkUploadFieldVisits(51, @"BulkUploadFieldVisits", @"Bulk Upload Field Visits");
    }

    public partial class NeptunePageTypeHippocampHomePage : NeptunePageType
    {
        private NeptunePageTypeHippocampHomePage(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampHomePage Instance = new NeptunePageTypeHippocampHomePage(52, @"HippocampHomePage", @"Hippocamp Home Page");
    }

    public partial class NeptunePageTypeHippocampTraining : NeptunePageType
    {
        private NeptunePageTypeHippocampTraining(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampTraining Instance = new NeptunePageTypeHippocampTraining(53, @"HippocampTraining", @"Hippocamp Training");
    }

    public partial class NeptunePageTypeHippocampLabelsAndDefinitionsList : NeptunePageType
    {
        private NeptunePageTypeHippocampLabelsAndDefinitionsList(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampLabelsAndDefinitionsList Instance = new NeptunePageTypeHippocampLabelsAndDefinitionsList(54, @"HippocampLabelsAndDefinitionsList", @"Hippocamp Labels and Definitions List");
    }

    public partial class NeptunePageTypeHippocampAbout : NeptunePageType
    {
        private NeptunePageTypeHippocampAbout(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampAbout Instance = new NeptunePageTypeHippocampAbout(55, @"HippocampAbout", @"Hippocamp About Page");
    }

    public partial class NeptunePageTypeHippocampProjectsList : NeptunePageType
    {
        private NeptunePageTypeHippocampProjectsList(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampProjectsList Instance = new NeptunePageTypeHippocampProjectsList(56, @"HippocampProjectsList", @"Hippocamp Projects List");
    }

    public partial class NeptunePageTypeHippocampProjectInstructions : NeptunePageType
    {
        private NeptunePageTypeHippocampProjectInstructions(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampProjectInstructions Instance = new NeptunePageTypeHippocampProjectInstructions(57, @"HippocampProjectInstructions", @"Hippocamp Project Instructions Page");
    }

    public partial class NeptunePageTypeHippocampProjectBasics : NeptunePageType
    {
        private NeptunePageTypeHippocampProjectBasics(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampProjectBasics Instance = new NeptunePageTypeHippocampProjectBasics(58, @"HippocampProjectBasics", @"Hippocamp Project Basics");
    }

    public partial class NeptunePageTypeHippocampProjectAttachments : NeptunePageType
    {
        private NeptunePageTypeHippocampProjectAttachments(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampProjectAttachments Instance = new NeptunePageTypeHippocampProjectAttachments(59, @"HippocampProjectAttachments", @"Hippocamp Project Attachments");
    }

    public partial class NeptunePageTypeHippocampTreatmentBMPs : NeptunePageType
    {
        private NeptunePageTypeHippocampTreatmentBMPs(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampTreatmentBMPs Instance = new NeptunePageTypeHippocampTreatmentBMPs(60, @"HippocampTreatmentBMPs", @"Hippocamp Treatment BMPs");
    }

    public partial class NeptunePageTypeHippocampDelineations : NeptunePageType
    {
        private NeptunePageTypeHippocampDelineations(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampDelineations Instance = new NeptunePageTypeHippocampDelineations(61, @"HippocampDelineations", @"Hippocamp Delineations");
    }

    public partial class NeptunePageTypeHippocampModeledPerformance : NeptunePageType
    {
        private NeptunePageTypeHippocampModeledPerformance(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampModeledPerformance Instance = new NeptunePageTypeHippocampModeledPerformance(62, @"HippocampModeledPerformance", @"Hippocamp Modeled Performance");
    }

    public partial class NeptunePageTypeHippocampReview : NeptunePageType
    {
        private NeptunePageTypeHippocampReview(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampReview Instance = new NeptunePageTypeHippocampReview(63, @"HippocampReview", @"Hippocamp Review");
    }

    public partial class NeptunePageTypeHippocampPlanningMap : NeptunePageType
    {
        private NeptunePageTypeHippocampPlanningMap(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeHippocampPlanningMap Instance = new NeptunePageTypeHippocampPlanningMap(64, @"HippocampPlanningMap", @"Hippocamp Planning Map");
    }

    public partial class NeptunePageTypeOCTAM2Tier2GrantProgramMetrics : NeptunePageType
    {
        private NeptunePageTypeOCTAM2Tier2GrantProgramMetrics(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeOCTAM2Tier2GrantProgramMetrics Instance = new NeptunePageTypeOCTAM2Tier2GrantProgramMetrics(65, @"OCTAM2Tier2GrantProgramMetrics", @"OCTA M2 Tier 2 Grant Program Metrics");
    }

    public partial class NeptunePageTypeOCTAM2Tier2GrantProgramDashboard : NeptunePageType
    {
        private NeptunePageTypeOCTAM2Tier2GrantProgramDashboard(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeOCTAM2Tier2GrantProgramDashboard Instance = new NeptunePageTypeOCTAM2Tier2GrantProgramDashboard(66, @"OCTAM2Tier2GrantProgramDashboard", @"OCTA M2 Tier 2 Grant Program Dashboard");
    }

    public partial class NeptunePageTypeEditWQMPBoundary : NeptunePageType
    {
        private NeptunePageTypeEditWQMPBoundary(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeEditWQMPBoundary Instance = new NeptunePageTypeEditWQMPBoundary(67, @"EditWQMPBoundary", @"Refine WQMP Boundary Area");
    }

    public partial class NeptunePageTypeUploadWQMPs : NeptunePageType
    {
        private NeptunePageTypeUploadWQMPs(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeUploadWQMPs Instance = new NeptunePageTypeUploadWQMPs(68, @"UploadWQMPs", @"Bulk Upload Water Quality Management Plans");
    }

    public partial class NeptunePageTypeUploadSimplifiedBMPs : NeptunePageType
    {
        private NeptunePageTypeUploadSimplifiedBMPs(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeUploadSimplifiedBMPs Instance = new NeptunePageTypeUploadSimplifiedBMPs(69, @"UploadSimplifiedBMPs", @"Bulk Upload Simplified BMPs");
    }

    public partial class NeptunePageTypeUploadOVTAs : NeptunePageType
    {
        private NeptunePageTypeUploadOVTAs(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeUploadOVTAs Instance = new NeptunePageTypeUploadOVTAs(70, @"UploadOVTAs", @"Bulk Upload OVTAs");
    }

    public partial class NeptunePageTypeWQMPBoundaryFromAPNList : NeptunePageType
    {
        private NeptunePageTypeWQMPBoundaryFromAPNList(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPBoundaryFromAPNList Instance = new NeptunePageTypeWQMPBoundaryFromAPNList(71, @"WQMPBoundaryFromAPNList", @"WQMP Boundary from APN List");
    }

    public partial class NeptunePageTypeBMPDataHub : NeptunePageType
    {
        private NeptunePageTypeBMPDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeBMPDataHub Instance = new NeptunePageTypeBMPDataHub(72, @"BMPDataHub", @"Treatment BMP Data Hub");
    }

    public partial class NeptunePageTypeDelineationDataHub : NeptunePageType
    {
        private NeptunePageTypeDelineationDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeDelineationDataHub Instance = new NeptunePageTypeDelineationDataHub(73, @"DelineationDataHub", @"Delineation Data Hub");
    }

    public partial class NeptunePageTypeFieldVisitDataHub : NeptunePageType
    {
        private NeptunePageTypeFieldVisitDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeFieldVisitDataHub Instance = new NeptunePageTypeFieldVisitDataHub(74, @"FieldVisitDataHub", @"Field Visit Data Hub");
    }

    public partial class NeptunePageTypeWQMPDataHub : NeptunePageType
    {
        private NeptunePageTypeWQMPDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPDataHub Instance = new NeptunePageTypeWQMPDataHub(75, @"WQMPDataHub", @"WQMP Data Hub");
    }

    public partial class NeptunePageTypeSimplifiedBMPsDataHub : NeptunePageType
    {
        private NeptunePageTypeSimplifiedBMPsDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeSimplifiedBMPsDataHub Instance = new NeptunePageTypeSimplifiedBMPsDataHub(76, @"SimplifiedBMPsDataHub", @"Simplified BMPs Data Hub");
    }

    public partial class NeptunePageTypeWQMPLocationsDataHub : NeptunePageType
    {
        private NeptunePageTypeWQMPLocationsDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPLocationsDataHub Instance = new NeptunePageTypeWQMPLocationsDataHub(77, @"WQMPLocationsDataHub", @"WQMP Locations Data Hub");
    }

    public partial class NeptunePageTypeAssessmentAreasDataHub : NeptunePageType
    {
        private NeptunePageTypeAssessmentAreasDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeAssessmentAreasDataHub Instance = new NeptunePageTypeAssessmentAreasDataHub(78, @"AssessmentAreasDataHub", @"Assessment Areas Data Hub");
    }

    public partial class NeptunePageTypeOVTADataHub : NeptunePageType
    {
        private NeptunePageTypeOVTADataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeOVTADataHub Instance = new NeptunePageTypeOVTADataHub(79, @"OVTADataHub", @"OVTA Data Hub");
    }

    public partial class NeptunePageTypeLandUseBlockDataHub : NeptunePageType
    {
        private NeptunePageTypeLandUseBlockDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeLandUseBlockDataHub Instance = new NeptunePageTypeLandUseBlockDataHub(80, @"LandUseBlockDataHub", @"Land Use Block Data Hub");
    }

    public partial class NeptunePageTypeRegionalSubbasinsDataHub : NeptunePageType
    {
        private NeptunePageTypeRegionalSubbasinsDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeRegionalSubbasinsDataHub Instance = new NeptunePageTypeRegionalSubbasinsDataHub(81, @"RegionalSubbasinsDataHub", @"Regional Subbasins Data Hub");
    }

    public partial class NeptunePageTypeLandUseStatisticsDataHub : NeptunePageType
    {
        private NeptunePageTypeLandUseStatisticsDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeLandUseStatisticsDataHub Instance = new NeptunePageTypeLandUseStatisticsDataHub(82, @"LandUseStatisticsDataHub", @"Land Use Statistics Data Hub");
    }

    public partial class NeptunePageTypeModelBasinsDataHub : NeptunePageType
    {
        private NeptunePageTypeModelBasinsDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeModelBasinsDataHub Instance = new NeptunePageTypeModelBasinsDataHub(83, @"ModelBasinsDataHub", @"Model Basins Data Hub");
    }

    public partial class NeptunePageTypePrecipitationZonesDataHub : NeptunePageType
    {
        private NeptunePageTypePrecipitationZonesDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypePrecipitationZonesDataHub Instance = new NeptunePageTypePrecipitationZonesDataHub(84, @"PrecipitationZonesDataHub", @"Precipitation Zones Data Hub");
    }

    public partial class NeptunePageTypeParcelUploadDataHub : NeptunePageType
    {
        private NeptunePageTypeParcelUploadDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeParcelUploadDataHub Instance = new NeptunePageTypeParcelUploadDataHub(85, @"ParcelUploadDataHub", @"Parcel Upload Data Hub");
    }

    public partial class NeptunePageTypeExportBMPInventoryToGIS : NeptunePageType
    {
        private NeptunePageTypeExportBMPInventoryToGIS(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeExportBMPInventoryToGIS Instance = new NeptunePageTypeExportBMPInventoryToGIS(86, @"ExportBMPInventoryToGIS", @"Export BMP Inventory to GIS");
    }

    public partial class NeptunePageTypeSPAHomePage : NeptunePageType
    {
        private NeptunePageTypeSPAHomePage(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeSPAHomePage Instance = new NeptunePageTypeSPAHomePage(87, @"SPAHomePage", @"SPA Home Page");
    }

    public partial class NeptunePageTypeWQMPModelingOptions : NeptunePageType
    {
        private NeptunePageTypeWQMPModelingOptions(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPModelingOptions Instance = new NeptunePageTypeWQMPModelingOptions(88, @"WQMPModelingOptions", @"WQMP Modeling Options");
    }

    public partial class NeptunePageTypeWQMPApprovalSummary : NeptunePageType
    {
        private NeptunePageTypeWQMPApprovalSummary(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPApprovalSummary Instance = new NeptunePageTypeWQMPApprovalSummary(89, @"WQMPApprovalSummary", @"WQMP Approval Summary");
    }

    public partial class NeptunePageTypeWQMPPostConstructionInspectionAndVerification : NeptunePageType
    {
        private NeptunePageTypeWQMPPostConstructionInspectionAndVerification(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPPostConstructionInspectionAndVerification Instance = new NeptunePageTypeWQMPPostConstructionInspectionAndVerification(90, @"WQMPPostConstructionInspectionAndVerification", @"WQMP Post Construction Inspection and Verification");
    }

    public partial class NeptunePageTypeWQMPMap : NeptunePageType
    {
        private NeptunePageTypeWQMPMap(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPMap Instance = new NeptunePageTypeWQMPMap(91, @"WQMPMap", @"WQMP Map");
    }

    public partial class NeptunePageTypeWQMPVerificationBasics : NeptunePageType
    {
        private NeptunePageTypeWQMPVerificationBasics(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPVerificationBasics Instance = new NeptunePageTypeWQMPVerificationBasics(93, @"WQMPVerificationBasics", @"WQMP O&M Verification - Basics Step");
    }

    public partial class NeptunePageTypeWQMPVerificationStructuralBmps : NeptunePageType
    {
        private NeptunePageTypeWQMPVerificationStructuralBmps(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPVerificationStructuralBmps Instance = new NeptunePageTypeWQMPVerificationStructuralBmps(94, @"WQMPVerificationStructuralBmps", @"WQMP O&M Verification - Structural BMPs Step");
    }

    public partial class NeptunePageTypeWQMPVerificationSimplifiedBmps : NeptunePageType
    {
        private NeptunePageTypeWQMPVerificationSimplifiedBmps(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPVerificationSimplifiedBmps Instance = new NeptunePageTypeWQMPVerificationSimplifiedBmps(95, @"WQMPVerificationSimplifiedBmps", @"WQMP O&M Verification - Simplified BMPs Step");
    }

    public partial class NeptunePageTypeWQMPVerificationSourceControl : NeptunePageType
    {
        private NeptunePageTypeWQMPVerificationSourceControl(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPVerificationSourceControl Instance = new NeptunePageTypeWQMPVerificationSourceControl(96, @"WQMPVerificationSourceControl", @"WQMP O&M Verification - Source Control BMPs Step");
    }

    public partial class NeptunePageTypeWQMPVerificationReview : NeptunePageType
    {
        private NeptunePageTypeWQMPVerificationReview(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPVerificationReview Instance = new NeptunePageTypeWQMPVerificationReview(97, @"WQMPVerificationReview", @"WQMP O&M Verification - Review & Finalize Step");
    }

    public partial class NeptunePageTypeWQMPVerificationSupportingDocumentation : NeptunePageType
    {
        private NeptunePageTypeWQMPVerificationSupportingDocumentation(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWQMPVerificationSupportingDocumentation Instance = new NeptunePageTypeWQMPVerificationSupportingDocumentation(98, @"WQMPVerificationSupportingDocumentation", @"WQMP O&M Verification - Supporting Documentation Step");
    }

    public partial class NeptunePageTypeWebServices : NeptunePageType
    {
        private NeptunePageTypeWebServices(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeWebServices Instance = new NeptunePageTypeWebServices(99, @"WebServices", @"Web Services");
    }

    public partial class NeptunePageTypeOCTAPrioritizationDataHub : NeptunePageType
    {
        private NeptunePageTypeOCTAPrioritizationDataHub(int neptunePageTypeID, string neptunePageTypeName, string neptunePageTypeDisplayName) : base(neptunePageTypeID, neptunePageTypeName, neptunePageTypeDisplayName) {}
        public static readonly NeptunePageTypeOCTAPrioritizationDataHub Instance = new NeptunePageTypeOCTAPrioritizationDataHub(100, @"OCTAPrioritizationDataHub", @"OCTA Prioritization Data Hub");
    }
}