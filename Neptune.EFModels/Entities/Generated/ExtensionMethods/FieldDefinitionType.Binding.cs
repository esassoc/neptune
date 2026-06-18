//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[FieldDefinitionType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class FieldDefinitionType : IHavePrimaryKey
    {
        public static readonly FieldDefinitionTypeIsPrimaryContactOrganization IsPrimaryContactOrganization = FieldDefinitionTypeIsPrimaryContactOrganization.Instance;
        public static readonly FieldDefinitionTypeOrganization Organization = FieldDefinitionTypeOrganization.Instance;
        public static readonly FieldDefinitionTypePassword Password = FieldDefinitionTypePassword.Instance;
        public static readonly FieldDefinitionTypeMeasurementUnit MeasurementUnit = FieldDefinitionTypeMeasurementUnit.Instance;
        public static readonly FieldDefinitionTypePhotoCaption PhotoCaption = FieldDefinitionTypePhotoCaption.Instance;
        public static readonly FieldDefinitionTypePhotoCredit PhotoCredit = FieldDefinitionTypePhotoCredit.Instance;
        public static readonly FieldDefinitionTypePhotoTiming PhotoTiming = FieldDefinitionTypePhotoTiming.Instance;
        public static readonly FieldDefinitionTypePrimaryContact PrimaryContact = FieldDefinitionTypePrimaryContact.Instance;
        public static readonly FieldDefinitionTypeOrganizationType OrganizationType = FieldDefinitionTypeOrganizationType.Instance;
        public static readonly FieldDefinitionTypeUsername Username = FieldDefinitionTypeUsername.Instance;
        public static readonly FieldDefinitionTypeExternalLinks ExternalLinks = FieldDefinitionTypeExternalLinks.Instance;
        public static readonly FieldDefinitionTypeRoleName RoleName = FieldDefinitionTypeRoleName.Instance;
        public static readonly FieldDefinitionTypeChartLastUpdatedDate ChartLastUpdatedDate = FieldDefinitionTypeChartLastUpdatedDate.Instance;
        public static readonly FieldDefinitionTypeTreatmentBMPType TreatmentBMPType = FieldDefinitionTypeTreatmentBMPType.Instance;
        public static readonly FieldDefinitionTypeConveyanceFunctionsAsIntended ConveyanceFunctionsAsIntended = FieldDefinitionTypeConveyanceFunctionsAsIntended.Instance;
        public static readonly FieldDefinitionTypeAssessmentScoreWeight AssessmentScoreWeight = FieldDefinitionTypeAssessmentScoreWeight.Instance;
        public static readonly FieldDefinitionTypeObservationScore ObservationScore = FieldDefinitionTypeObservationScore.Instance;
        public static readonly FieldDefinitionTypeAlternativeScore AlternativeScore = FieldDefinitionTypeAlternativeScore.Instance;
        public static readonly FieldDefinitionTypeAssessmentForInternalUseOnly AssessmentForInternalUseOnly = FieldDefinitionTypeAssessmentForInternalUseOnly.Instance;
        public static readonly FieldDefinitionTypeTreatmentBMPDesignDepth TreatmentBMPDesignDepth = FieldDefinitionTypeTreatmentBMPDesignDepth.Instance;
        public static readonly FieldDefinitionTypeReceivesSystemCommunications ReceivesSystemCommunications = FieldDefinitionTypeReceivesSystemCommunications.Instance;
        public static readonly FieldDefinitionTypeJurisdiction Jurisdiction = FieldDefinitionTypeJurisdiction.Instance;
        public static readonly FieldDefinitionTypeDelineation Delineation = FieldDefinitionTypeDelineation.Instance;
        public static readonly FieldDefinitionTypeTreatmentBMP TreatmentBMP = FieldDefinitionTypeTreatmentBMP.Instance;
        public static readonly FieldDefinitionTypeTreatmentBMPAssessmentObservationType TreatmentBMPAssessmentObservationType = FieldDefinitionTypeTreatmentBMPAssessmentObservationType.Instance;
        public static readonly FieldDefinitionTypeObservationCollectionMethod ObservationCollectionMethod = FieldDefinitionTypeObservationCollectionMethod.Instance;
        public static readonly FieldDefinitionTypeObservationThresholdType ObservationThresholdType = FieldDefinitionTypeObservationThresholdType.Instance;
        public static readonly FieldDefinitionTypeObservationTargetType ObservationTargetType = FieldDefinitionTypeObservationTargetType.Instance;
        public static readonly FieldDefinitionTypeMeasurementUnitLabel MeasurementUnitLabel = FieldDefinitionTypeMeasurementUnitLabel.Instance;
        public static readonly FieldDefinitionTypePropertiesToObserve PropertiesToObserve = FieldDefinitionTypePropertiesToObserve.Instance;
        public static readonly FieldDefinitionTypeMinimumNumberOfObservations MinimumNumberOfObservations = FieldDefinitionTypeMinimumNumberOfObservations.Instance;
        public static readonly FieldDefinitionTypeMaximumNumberOfObservations MaximumNumberOfObservations = FieldDefinitionTypeMaximumNumberOfObservations.Instance;
        public static readonly FieldDefinitionTypeMinimumValueOfEachObservation MinimumValueOfEachObservation = FieldDefinitionTypeMinimumValueOfEachObservation.Instance;
        public static readonly FieldDefinitionTypeMaximumValueOfEachObservation MaximumValueOfEachObservation = FieldDefinitionTypeMaximumValueOfEachObservation.Instance;
        public static readonly FieldDefinitionTypeDefaultThresholdValue DefaultThresholdValue = FieldDefinitionTypeDefaultThresholdValue.Instance;
        public static readonly FieldDefinitionTypeDefaultBenchmarkValue DefaultBenchmarkValue = FieldDefinitionTypeDefaultBenchmarkValue.Instance;
        public static readonly FieldDefinitionTypeAssessmentFailsIfObservationFails AssessmentFailsIfObservationFails = FieldDefinitionTypeAssessmentFailsIfObservationFails.Instance;
        public static readonly FieldDefinitionTypeCustomAttributeType CustomAttributeType = FieldDefinitionTypeCustomAttributeType.Instance;
        public static readonly FieldDefinitionTypeCustomAttributeDataType CustomAttributeDataType = FieldDefinitionTypeCustomAttributeDataType.Instance;
        public static readonly FieldDefinitionTypeMaintenanceRecordType MaintenanceRecordType = FieldDefinitionTypeMaintenanceRecordType.Instance;
        public static readonly FieldDefinitionTypeMaintenanceRecord MaintenanceRecord = FieldDefinitionTypeMaintenanceRecord.Instance;
        public static readonly FieldDefinitionTypeAttributeTypePurpose AttributeTypePurpose = FieldDefinitionTypeAttributeTypePurpose.Instance;
        public static readonly FieldDefinitionTypeFundingSource FundingSource = FieldDefinitionTypeFundingSource.Instance;
        public static readonly FieldDefinitionTypeIsPostMaintenanceAssessment IsPostMaintenanceAssessment = FieldDefinitionTypeIsPostMaintenanceAssessment.Instance;
        public static readonly FieldDefinitionTypeFundingEvent FundingEvent = FieldDefinitionTypeFundingEvent.Instance;
        public static readonly FieldDefinitionTypeFieldVisit FieldVisit = FieldDefinitionTypeFieldVisit.Instance;
        public static readonly FieldDefinitionTypeFieldVisitStatus FieldVisitStatus = FieldDefinitionTypeFieldVisitStatus.Instance;
        public static readonly FieldDefinitionTypeWaterQualityManagementPlan WaterQualityManagementPlan = FieldDefinitionTypeWaterQualityManagementPlan.Instance;
        public static readonly FieldDefinitionTypeParcel Parcel = FieldDefinitionTypeParcel.Instance;
        public static readonly FieldDefinitionTypeRequiredLifespanOfInstallation RequiredLifespanOfInstallation = FieldDefinitionTypeRequiredLifespanOfInstallation.Instance;
        public static readonly FieldDefinitionTypeRequiredFieldVisitsPerYear RequiredFieldVisitsPerYear = FieldDefinitionTypeRequiredFieldVisitsPerYear.Instance;
        public static readonly FieldDefinitionTypeRequiredPostStormFieldVisitsPerYear RequiredPostStormFieldVisitsPerYear = FieldDefinitionTypeRequiredPostStormFieldVisitsPerYear.Instance;
        public static readonly FieldDefinitionTypeWaterQualityManagementPlanDocumentType WaterQualityManagementPlanDocumentType = FieldDefinitionTypeWaterQualityManagementPlanDocumentType.Instance;
        public static readonly FieldDefinitionTypeHasAllRequiredDocuments HasAllRequiredDocuments = FieldDefinitionTypeHasAllRequiredDocuments.Instance;
        public static readonly FieldDefinitionTypeDateOfLastInventoryChange DateOfLastInventoryChange = FieldDefinitionTypeDateOfLastInventoryChange.Instance;
        public static readonly FieldDefinitionTypeTrashCaptureStatus TrashCaptureStatus = FieldDefinitionTypeTrashCaptureStatus.Instance;
        public static readonly FieldDefinitionTypeOnlandVisualTrashAssessment OnlandVisualTrashAssessment = FieldDefinitionTypeOnlandVisualTrashAssessment.Instance;
        public static readonly FieldDefinitionTypeOnlandVisualTrashAssessmentNotes OnlandVisualTrashAssessmentNotes = FieldDefinitionTypeOnlandVisualTrashAssessmentNotes.Instance;
        public static readonly FieldDefinitionTypeDelineationType DelineationType = FieldDefinitionTypeDelineationType.Instance;
        public static readonly FieldDefinitionTypeBaselineScore BaselineScore = FieldDefinitionTypeBaselineScore.Instance;
        public static readonly FieldDefinitionTypeSizingBasis SizingBasis = FieldDefinitionTypeSizingBasis.Instance;
        public static readonly FieldDefinitionTypeProgressScore ProgressScore = FieldDefinitionTypeProgressScore.Instance;
        public static readonly FieldDefinitionTypeAssessmentScore AssessmentScore = FieldDefinitionTypeAssessmentScore.Instance;
        public static readonly FieldDefinitionTypeViaFullCapture ViaFullCapture = FieldDefinitionTypeViaFullCapture.Instance;
        public static readonly FieldDefinitionTypeViaPartialCapture ViaPartialCapture = FieldDefinitionTypeViaPartialCapture.Instance;
        public static readonly FieldDefinitionTypeViaOVTAScore ViaOVTAScore = FieldDefinitionTypeViaOVTAScore.Instance;
        public static readonly FieldDefinitionTypeTotalAchieved TotalAchieved = FieldDefinitionTypeTotalAchieved.Instance;
        public static readonly FieldDefinitionTypeTargetLoadReduction TargetLoadReduction = FieldDefinitionTypeTargetLoadReduction.Instance;
        public static readonly FieldDefinitionTypeTrashGenerationRate TrashGenerationRate = FieldDefinitionTypeTrashGenerationRate.Instance;
        public static readonly FieldDefinitionTypeLandUse LandUse = FieldDefinitionTypeLandUse.Instance;
        public static readonly FieldDefinitionTypeArea Area = FieldDefinitionTypeArea.Instance;
        public static readonly FieldDefinitionTypeImperviousArea ImperviousArea = FieldDefinitionTypeImperviousArea.Instance;
        public static readonly FieldDefinitionTypeGrossArea GrossArea = FieldDefinitionTypeGrossArea.Instance;
        public static readonly FieldDefinitionTypeLandUseStatistics LandUseStatistics = FieldDefinitionTypeLandUseStatistics.Instance;
        public static readonly FieldDefinitionTypeRegionalSubbasin RegionalSubbasin = FieldDefinitionTypeRegionalSubbasin.Instance;
        public static readonly FieldDefinitionTypeAverageDivertedFlowrate AverageDivertedFlowrate = FieldDefinitionTypeAverageDivertedFlowrate.Instance;
        public static readonly FieldDefinitionTypeAverageTreatmentFlowrate AverageTreatmentFlowrate = FieldDefinitionTypeAverageTreatmentFlowrate.Instance;
        public static readonly FieldDefinitionTypeDesignDryWeatherTreatmentCapacity DesignDryWeatherTreatmentCapacity = FieldDefinitionTypeDesignDryWeatherTreatmentCapacity.Instance;
        public static readonly FieldDefinitionTypeDesignLowFlowDiversionCapacity DesignLowFlowDiversionCapacity = FieldDefinitionTypeDesignLowFlowDiversionCapacity.Instance;
        public static readonly FieldDefinitionTypeDesignMediaFiltrationRate DesignMediaFiltrationRate = FieldDefinitionTypeDesignMediaFiltrationRate.Instance;
        public static readonly FieldDefinitionTypeDesignResidenceTimeForPermanentPool DesignResidenceTimeForPermanentPool = FieldDefinitionTypeDesignResidenceTimeForPermanentPool.Instance;
        public static readonly FieldDefinitionTypeDiversionRate DiversionRate = FieldDefinitionTypeDiversionRate.Instance;
        public static readonly FieldDefinitionTypeDrawdownTimeForWQDetentionVolume DrawdownTimeForWQDetentionVolume = FieldDefinitionTypeDrawdownTimeForWQDetentionVolume.Instance;
        public static readonly FieldDefinitionTypeEffectiveFootprint EffectiveFootprint = FieldDefinitionTypeEffectiveFootprint.Instance;
        public static readonly FieldDefinitionTypeEffectiveRetentionDepth EffectiveRetentionDepth = FieldDefinitionTypeEffectiveRetentionDepth.Instance;
        public static readonly FieldDefinitionTypeInfiltrationDischargeRate InfiltrationDischargeRate = FieldDefinitionTypeInfiltrationDischargeRate.Instance;
        public static readonly FieldDefinitionTypeInfiltrationSurfaceArea InfiltrationSurfaceArea = FieldDefinitionTypeInfiltrationSurfaceArea.Instance;
        public static readonly FieldDefinitionTypeMediaBedFootprint MediaBedFootprint = FieldDefinitionTypeMediaBedFootprint.Instance;
        public static readonly FieldDefinitionTypeMonthsOfOperationID MonthsOfOperationID = FieldDefinitionTypeMonthsOfOperationID.Instance;
        public static readonly FieldDefinitionTypePermanentPoolOrWetlandVolume PermanentPoolOrWetlandVolume = FieldDefinitionTypePermanentPoolOrWetlandVolume.Instance;
        public static readonly FieldDefinitionTypeRoutingConfiguration RoutingConfiguration = FieldDefinitionTypeRoutingConfiguration.Instance;
        public static readonly FieldDefinitionTypeStorageVolumeBelowLowestOutletElevation StorageVolumeBelowLowestOutletElevation = FieldDefinitionTypeStorageVolumeBelowLowestOutletElevation.Instance;
        public static readonly FieldDefinitionTypeSummerHarvestedWaterDemand SummerHarvestedWaterDemand = FieldDefinitionTypeSummerHarvestedWaterDemand.Instance;
        public static readonly FieldDefinitionTypeTimeOfConcentrationID TimeOfConcentrationID = FieldDefinitionTypeTimeOfConcentrationID.Instance;
        public static readonly FieldDefinitionTypeDrawdownTimeForDetentionVolume DrawdownTimeForDetentionVolume = FieldDefinitionTypeDrawdownTimeForDetentionVolume.Instance;
        public static readonly FieldDefinitionTypeTotalEffectiveBMPVolume TotalEffectiveBMPVolume = FieldDefinitionTypeTotalEffectiveBMPVolume.Instance;
        public static readonly FieldDefinitionTypeTotalEffectiveDrywellBMPVolume TotalEffectiveDrywellBMPVolume = FieldDefinitionTypeTotalEffectiveDrywellBMPVolume.Instance;
        public static readonly FieldDefinitionTypeTreatmentRate TreatmentRate = FieldDefinitionTypeTreatmentRate.Instance;
        public static readonly FieldDefinitionTypeUnderlyingHydrologicSoilGroupID UnderlyingHydrologicSoilGroupID = FieldDefinitionTypeUnderlyingHydrologicSoilGroupID.Instance;
        public static readonly FieldDefinitionTypeUnderlyingInfiltrationRate UnderlyingInfiltrationRate = FieldDefinitionTypeUnderlyingInfiltrationRate.Instance;
        public static readonly FieldDefinitionTypeUpstreamBMP UpstreamBMP = FieldDefinitionTypeUpstreamBMP.Instance;
        public static readonly FieldDefinitionTypeExtendedDetentionSurchargeVolume ExtendedDetentionSurchargeVolume = FieldDefinitionTypeExtendedDetentionSurchargeVolume.Instance;
        public static readonly FieldDefinitionTypeWettedFootprint WettedFootprint = FieldDefinitionTypeWettedFootprint.Instance;
        public static readonly FieldDefinitionTypeWinterHarvestedWaterDemand WinterHarvestedWaterDemand = FieldDefinitionTypeWinterHarvestedWaterDemand.Instance;
        public static readonly FieldDefinitionTypePercentOfSiteTreated PercentOfSiteTreated = FieldDefinitionTypePercentOfSiteTreated.Instance;
        public static readonly FieldDefinitionTypePercentCaptured PercentCaptured = FieldDefinitionTypePercentCaptured.Instance;
        public static readonly FieldDefinitionTypePercentRetained PercentRetained = FieldDefinitionTypePercentRetained.Instance;
        public static readonly FieldDefinitionTypeAreaWithinWQMP AreaWithinWQMP = FieldDefinitionTypeAreaWithinWQMP.Instance;
        public static readonly FieldDefinitionTypeWatershed Watershed = FieldDefinitionTypeWatershed.Instance;
        public static readonly FieldDefinitionTypeDesignStormwaterDepth DesignStormwaterDepth = FieldDefinitionTypeDesignStormwaterDepth.Instance;
        public static readonly FieldDefinitionTypeFullyParameterized FullyParameterized = FieldDefinitionTypeFullyParameterized.Instance;
        public static readonly FieldDefinitionTypeHydromodificationApplies HydromodificationApplies = FieldDefinitionTypeHydromodificationApplies.Instance;
        public static readonly FieldDefinitionTypeDelineationStatus DelineationStatus = FieldDefinitionTypeDelineationStatus.Instance;
        public static readonly FieldDefinitionTypeDryWeatherFlowOverrideID DryWeatherFlowOverrideID = FieldDefinitionTypeDryWeatherFlowOverrideID.Instance;
        public static readonly FieldDefinitionTypeModeledPerformance ModeledPerformance = FieldDefinitionTypeModeledPerformance.Instance;
        public static readonly FieldDefinitionTypeOCTAM2Tier2GrantProgram OCTAM2Tier2GrantProgram = FieldDefinitionTypeOCTAM2Tier2GrantProgram.Instance;
        public static readonly FieldDefinitionTypeSEAScore SEAScore = FieldDefinitionTypeSEAScore.Instance;
        public static readonly FieldDefinitionTypeTPIScore TPIScore = FieldDefinitionTypeTPIScore.Instance;
        public static readonly FieldDefinitionTypeWQLRI WQLRI = FieldDefinitionTypeWQLRI.Instance;
        public static readonly FieldDefinitionTypePollutantContributiontoSEA PollutantContributiontoSEA = FieldDefinitionTypePollutantContributiontoSEA.Instance;
        public static readonly FieldDefinitionTypeSiteRunoff SiteRunoff = FieldDefinitionTypeSiteRunoff.Instance;
        public static readonly FieldDefinitionTypeTreatedAndDischarged TreatedAndDischarged = FieldDefinitionTypeTreatedAndDischarged.Instance;
        public static readonly FieldDefinitionTypeRetainedOrRecycled RetainedOrRecycled = FieldDefinitionTypeRetainedOrRecycled.Instance;
        public static readonly FieldDefinitionTypeUntreatedBypassOrOverflow UntreatedBypassOrOverflow = FieldDefinitionTypeUntreatedBypassOrOverflow.Instance;
        public static readonly FieldDefinitionTypeTotalSuspendedSolids TotalSuspendedSolids = FieldDefinitionTypeTotalSuspendedSolids.Instance;
        public static readonly FieldDefinitionTypeTotalNitrogen TotalNitrogen = FieldDefinitionTypeTotalNitrogen.Instance;
        public static readonly FieldDefinitionTypeTotalPhosphorous TotalPhosphorous = FieldDefinitionTypeTotalPhosphorous.Instance;
        public static readonly FieldDefinitionTypeFecalColiform FecalColiform = FieldDefinitionTypeFecalColiform.Instance;
        public static readonly FieldDefinitionTypeTotalCopper TotalCopper = FieldDefinitionTypeTotalCopper.Instance;
        public static readonly FieldDefinitionTypeTotalLead TotalLead = FieldDefinitionTypeTotalLead.Instance;
        public static readonly FieldDefinitionTypeTotalZinc TotalZinc = FieldDefinitionTypeTotalZinc.Instance;
        public static readonly FieldDefinitionTypeOCTAWatershed OCTAWatershed = FieldDefinitionTypeOCTAWatershed.Instance;
        public static readonly FieldDefinitionTypeEffectiveAreaAcres EffectiveAreaAcres = FieldDefinitionTypeEffectiveAreaAcres.Instance;
        public static readonly FieldDefinitionTypeDesignStormDepth85thPercentile DesignStormDepth85thPercentile = FieldDefinitionTypeDesignStormDepth85thPercentile.Instance;
        public static readonly FieldDefinitionTypeDesignVolume85thPercentile DesignVolume85thPercentile = FieldDefinitionTypeDesignVolume85thPercentile.Instance;
        public static readonly FieldDefinitionTypeLandUseBasedWaterQualityScore LandUseBasedWaterQualityScore = FieldDefinitionTypeLandUseBasedWaterQualityScore.Instance;
        public static readonly FieldDefinitionTypeReceivingWaterScore ReceivingWaterScore = FieldDefinitionTypeReceivingWaterScore.Instance;
        public static readonly FieldDefinitionTypeNumberOfIndividualBMPs NumberOfIndividualBMPs = FieldDefinitionTypeNumberOfIndividualBMPs.Instance;
        public static readonly FieldDefinitionTypeLandUseType LandUseType = FieldDefinitionTypeLandUseType.Instance;
        public static readonly FieldDefinitionTypeLandUseDescription LandUseDescription = FieldDefinitionTypeLandUseDescription.Instance;
        public static readonly FieldDefinitionTypeOVTAScoreEquivalentCalculation OVTAScoreEquivalentCalculation = FieldDefinitionTypeOVTAScoreEquivalentCalculation.Instance;
        public static readonly FieldDefinitionTypeBaselineLoadingRate BaselineLoadingRate = FieldDefinitionTypeBaselineLoadingRate.Instance;
        public static readonly FieldDefinitionTypeCurrentLoadingRate CurrentLoadingRate = FieldDefinitionTypeCurrentLoadingRate.Instance;
        public static readonly FieldDefinitionTypeBMPTrashTreatmentStatus BMPTrashTreatmentStatus = FieldDefinitionTypeBMPTrashTreatmentStatus.Instance;
        public static readonly FieldDefinitionTypeWQMPTrashTreatmentStatus WQMPTrashTreatmentStatus = FieldDefinitionTypeWQMPTrashTreatmentStatus.Instance;
        public static readonly FieldDefinitionTypeDownstreamOfNonModeledBMP DownstreamOfNonModeledBMP = FieldDefinitionTypeDownstreamOfNonModeledBMP.Instance;

        public static readonly List<FieldDefinitionType> All;
        public static readonly ReadOnlyDictionary<int, FieldDefinitionType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static FieldDefinitionType()
        {
            All = new List<FieldDefinitionType> { IsPrimaryContactOrganization, Organization, Password, MeasurementUnit, PhotoCaption, PhotoCredit, PhotoTiming, PrimaryContact, OrganizationType, Username, ExternalLinks, RoleName, ChartLastUpdatedDate, TreatmentBMPType, ConveyanceFunctionsAsIntended, AssessmentScoreWeight, ObservationScore, AlternativeScore, AssessmentForInternalUseOnly, TreatmentBMPDesignDepth, ReceivesSystemCommunications, Jurisdiction, Delineation, TreatmentBMP, TreatmentBMPAssessmentObservationType, ObservationCollectionMethod, ObservationThresholdType, ObservationTargetType, MeasurementUnitLabel, PropertiesToObserve, MinimumNumberOfObservations, MaximumNumberOfObservations, MinimumValueOfEachObservation, MaximumValueOfEachObservation, DefaultThresholdValue, DefaultBenchmarkValue, AssessmentFailsIfObservationFails, CustomAttributeType, CustomAttributeDataType, MaintenanceRecordType, MaintenanceRecord, AttributeTypePurpose, FundingSource, IsPostMaintenanceAssessment, FundingEvent, FieldVisit, FieldVisitStatus, WaterQualityManagementPlan, Parcel, RequiredLifespanOfInstallation, RequiredFieldVisitsPerYear, RequiredPostStormFieldVisitsPerYear, WaterQualityManagementPlanDocumentType, HasAllRequiredDocuments, DateOfLastInventoryChange, TrashCaptureStatus, OnlandVisualTrashAssessment, OnlandVisualTrashAssessmentNotes, DelineationType, BaselineScore, SizingBasis, ProgressScore, AssessmentScore, ViaFullCapture, ViaPartialCapture, ViaOVTAScore, TotalAchieved, TargetLoadReduction, TrashGenerationRate, LandUse, Area, ImperviousArea, GrossArea, LandUseStatistics, RegionalSubbasin, AverageDivertedFlowrate, AverageTreatmentFlowrate, DesignDryWeatherTreatmentCapacity, DesignLowFlowDiversionCapacity, DesignMediaFiltrationRate, DesignResidenceTimeForPermanentPool, DiversionRate, DrawdownTimeForWQDetentionVolume, EffectiveFootprint, EffectiveRetentionDepth, InfiltrationDischargeRate, InfiltrationSurfaceArea, MediaBedFootprint, MonthsOfOperationID, PermanentPoolOrWetlandVolume, RoutingConfiguration, StorageVolumeBelowLowestOutletElevation, SummerHarvestedWaterDemand, TimeOfConcentrationID, DrawdownTimeForDetentionVolume, TotalEffectiveBMPVolume, TotalEffectiveDrywellBMPVolume, TreatmentRate, UnderlyingHydrologicSoilGroupID, UnderlyingInfiltrationRate, UpstreamBMP, ExtendedDetentionSurchargeVolume, WettedFootprint, WinterHarvestedWaterDemand, PercentOfSiteTreated, PercentCaptured, PercentRetained, AreaWithinWQMP, Watershed, DesignStormwaterDepth, FullyParameterized, HydromodificationApplies, DelineationStatus, DryWeatherFlowOverrideID, ModeledPerformance, OCTAM2Tier2GrantProgram, SEAScore, TPIScore, WQLRI, PollutantContributiontoSEA, SiteRunoff, TreatedAndDischarged, RetainedOrRecycled, UntreatedBypassOrOverflow, TotalSuspendedSolids, TotalNitrogen, TotalPhosphorous, FecalColiform, TotalCopper, TotalLead, TotalZinc, OCTAWatershed, EffectiveAreaAcres, DesignStormDepth85thPercentile, DesignVolume85thPercentile, LandUseBasedWaterQualityScore, ReceivingWaterScore, NumberOfIndividualBMPs, LandUseType, LandUseDescription, OVTAScoreEquivalentCalculation, BaselineLoadingRate, CurrentLoadingRate, BMPTrashTreatmentStatus, WQMPTrashTreatmentStatus, DownstreamOfNonModeledBMP };
            AllLookupDictionary = new ReadOnlyDictionary<int, FieldDefinitionType>(All.ToDictionary(x => x.FieldDefinitionTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected FieldDefinitionType(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName)
        {
            FieldDefinitionTypeID = fieldDefinitionTypeID;
            FieldDefinitionTypeName = fieldDefinitionTypeName;
            FieldDefinitionTypeDisplayName = fieldDefinitionTypeDisplayName;
        }

        [Key]
        public int FieldDefinitionTypeID { get; private set; }
        public string FieldDefinitionTypeName { get; private set; }
        public string FieldDefinitionTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return FieldDefinitionTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(FieldDefinitionType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.FieldDefinitionTypeID == FieldDefinitionTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as FieldDefinitionType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return FieldDefinitionTypeID;
        }

        public static bool operator ==(FieldDefinitionType left, FieldDefinitionType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FieldDefinitionType left, FieldDefinitionType right)
        {
            return !Equals(left, right);
        }

        public FieldDefinitionTypeEnum ToEnum => (FieldDefinitionTypeEnum)GetHashCode();

        public static FieldDefinitionType ToType(int enumValue)
        {
            return ToType((FieldDefinitionTypeEnum)enumValue);
        }

        public static FieldDefinitionType ToType(FieldDefinitionTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case FieldDefinitionTypeEnum.AlternativeScore:
                    return AlternativeScore;
                case FieldDefinitionTypeEnum.Area:
                    return Area;
                case FieldDefinitionTypeEnum.AreaWithinWQMP:
                    return AreaWithinWQMP;
                case FieldDefinitionTypeEnum.AssessmentFailsIfObservationFails:
                    return AssessmentFailsIfObservationFails;
                case FieldDefinitionTypeEnum.AssessmentForInternalUseOnly:
                    return AssessmentForInternalUseOnly;
                case FieldDefinitionTypeEnum.AssessmentScore:
                    return AssessmentScore;
                case FieldDefinitionTypeEnum.AssessmentScoreWeight:
                    return AssessmentScoreWeight;
                case FieldDefinitionTypeEnum.AttributeTypePurpose:
                    return AttributeTypePurpose;
                case FieldDefinitionTypeEnum.AverageDivertedFlowrate:
                    return AverageDivertedFlowrate;
                case FieldDefinitionTypeEnum.AverageTreatmentFlowrate:
                    return AverageTreatmentFlowrate;
                case FieldDefinitionTypeEnum.BaselineLoadingRate:
                    return BaselineLoadingRate;
                case FieldDefinitionTypeEnum.BaselineScore:
                    return BaselineScore;
                case FieldDefinitionTypeEnum.BMPTrashTreatmentStatus:
                    return BMPTrashTreatmentStatus;
                case FieldDefinitionTypeEnum.ChartLastUpdatedDate:
                    return ChartLastUpdatedDate;
                case FieldDefinitionTypeEnum.ConveyanceFunctionsAsIntended:
                    return ConveyanceFunctionsAsIntended;
                case FieldDefinitionTypeEnum.CurrentLoadingRate:
                    return CurrentLoadingRate;
                case FieldDefinitionTypeEnum.CustomAttributeDataType:
                    return CustomAttributeDataType;
                case FieldDefinitionTypeEnum.CustomAttributeType:
                    return CustomAttributeType;
                case FieldDefinitionTypeEnum.DateOfLastInventoryChange:
                    return DateOfLastInventoryChange;
                case FieldDefinitionTypeEnum.DefaultBenchmarkValue:
                    return DefaultBenchmarkValue;
                case FieldDefinitionTypeEnum.DefaultThresholdValue:
                    return DefaultThresholdValue;
                case FieldDefinitionTypeEnum.Delineation:
                    return Delineation;
                case FieldDefinitionTypeEnum.DelineationStatus:
                    return DelineationStatus;
                case FieldDefinitionTypeEnum.DelineationType:
                    return DelineationType;
                case FieldDefinitionTypeEnum.DesignDryWeatherTreatmentCapacity:
                    return DesignDryWeatherTreatmentCapacity;
                case FieldDefinitionTypeEnum.DesignLowFlowDiversionCapacity:
                    return DesignLowFlowDiversionCapacity;
                case FieldDefinitionTypeEnum.DesignMediaFiltrationRate:
                    return DesignMediaFiltrationRate;
                case FieldDefinitionTypeEnum.DesignResidenceTimeForPermanentPool:
                    return DesignResidenceTimeForPermanentPool;
                case FieldDefinitionTypeEnum.DesignStormDepth85thPercentile:
                    return DesignStormDepth85thPercentile;
                case FieldDefinitionTypeEnum.DesignStormwaterDepth:
                    return DesignStormwaterDepth;
                case FieldDefinitionTypeEnum.DesignVolume85thPercentile:
                    return DesignVolume85thPercentile;
                case FieldDefinitionTypeEnum.DiversionRate:
                    return DiversionRate;
                case FieldDefinitionTypeEnum.DownstreamOfNonModeledBMP:
                    return DownstreamOfNonModeledBMP;
                case FieldDefinitionTypeEnum.DrawdownTimeForDetentionVolume:
                    return DrawdownTimeForDetentionVolume;
                case FieldDefinitionTypeEnum.DrawdownTimeForWQDetentionVolume:
                    return DrawdownTimeForWQDetentionVolume;
                case FieldDefinitionTypeEnum.DryWeatherFlowOverrideID:
                    return DryWeatherFlowOverrideID;
                case FieldDefinitionTypeEnum.EffectiveAreaAcres:
                    return EffectiveAreaAcres;
                case FieldDefinitionTypeEnum.EffectiveFootprint:
                    return EffectiveFootprint;
                case FieldDefinitionTypeEnum.EffectiveRetentionDepth:
                    return EffectiveRetentionDepth;
                case FieldDefinitionTypeEnum.ExtendedDetentionSurchargeVolume:
                    return ExtendedDetentionSurchargeVolume;
                case FieldDefinitionTypeEnum.ExternalLinks:
                    return ExternalLinks;
                case FieldDefinitionTypeEnum.FecalColiform:
                    return FecalColiform;
                case FieldDefinitionTypeEnum.FieldVisit:
                    return FieldVisit;
                case FieldDefinitionTypeEnum.FieldVisitStatus:
                    return FieldVisitStatus;
                case FieldDefinitionTypeEnum.FullyParameterized:
                    return FullyParameterized;
                case FieldDefinitionTypeEnum.FundingEvent:
                    return FundingEvent;
                case FieldDefinitionTypeEnum.FundingSource:
                    return FundingSource;
                case FieldDefinitionTypeEnum.GrossArea:
                    return GrossArea;
                case FieldDefinitionTypeEnum.HasAllRequiredDocuments:
                    return HasAllRequiredDocuments;
                case FieldDefinitionTypeEnum.HydromodificationApplies:
                    return HydromodificationApplies;
                case FieldDefinitionTypeEnum.ImperviousArea:
                    return ImperviousArea;
                case FieldDefinitionTypeEnum.InfiltrationDischargeRate:
                    return InfiltrationDischargeRate;
                case FieldDefinitionTypeEnum.InfiltrationSurfaceArea:
                    return InfiltrationSurfaceArea;
                case FieldDefinitionTypeEnum.IsPostMaintenanceAssessment:
                    return IsPostMaintenanceAssessment;
                case FieldDefinitionTypeEnum.IsPrimaryContactOrganization:
                    return IsPrimaryContactOrganization;
                case FieldDefinitionTypeEnum.Jurisdiction:
                    return Jurisdiction;
                case FieldDefinitionTypeEnum.LandUse:
                    return LandUse;
                case FieldDefinitionTypeEnum.LandUseBasedWaterQualityScore:
                    return LandUseBasedWaterQualityScore;
                case FieldDefinitionTypeEnum.LandUseDescription:
                    return LandUseDescription;
                case FieldDefinitionTypeEnum.LandUseStatistics:
                    return LandUseStatistics;
                case FieldDefinitionTypeEnum.LandUseType:
                    return LandUseType;
                case FieldDefinitionTypeEnum.MaintenanceRecord:
                    return MaintenanceRecord;
                case FieldDefinitionTypeEnum.MaintenanceRecordType:
                    return MaintenanceRecordType;
                case FieldDefinitionTypeEnum.MaximumNumberOfObservations:
                    return MaximumNumberOfObservations;
                case FieldDefinitionTypeEnum.MaximumValueOfEachObservation:
                    return MaximumValueOfEachObservation;
                case FieldDefinitionTypeEnum.MeasurementUnit:
                    return MeasurementUnit;
                case FieldDefinitionTypeEnum.MeasurementUnitLabel:
                    return MeasurementUnitLabel;
                case FieldDefinitionTypeEnum.MediaBedFootprint:
                    return MediaBedFootprint;
                case FieldDefinitionTypeEnum.MinimumNumberOfObservations:
                    return MinimumNumberOfObservations;
                case FieldDefinitionTypeEnum.MinimumValueOfEachObservation:
                    return MinimumValueOfEachObservation;
                case FieldDefinitionTypeEnum.ModeledPerformance:
                    return ModeledPerformance;
                case FieldDefinitionTypeEnum.MonthsOfOperationID:
                    return MonthsOfOperationID;
                case FieldDefinitionTypeEnum.NumberOfIndividualBMPs:
                    return NumberOfIndividualBMPs;
                case FieldDefinitionTypeEnum.ObservationCollectionMethod:
                    return ObservationCollectionMethod;
                case FieldDefinitionTypeEnum.ObservationScore:
                    return ObservationScore;
                case FieldDefinitionTypeEnum.ObservationTargetType:
                    return ObservationTargetType;
                case FieldDefinitionTypeEnum.ObservationThresholdType:
                    return ObservationThresholdType;
                case FieldDefinitionTypeEnum.OCTAM2Tier2GrantProgram:
                    return OCTAM2Tier2GrantProgram;
                case FieldDefinitionTypeEnum.OCTAWatershed:
                    return OCTAWatershed;
                case FieldDefinitionTypeEnum.OnlandVisualTrashAssessment:
                    return OnlandVisualTrashAssessment;
                case FieldDefinitionTypeEnum.OnlandVisualTrashAssessmentNotes:
                    return OnlandVisualTrashAssessmentNotes;
                case FieldDefinitionTypeEnum.Organization:
                    return Organization;
                case FieldDefinitionTypeEnum.OrganizationType:
                    return OrganizationType;
                case FieldDefinitionTypeEnum.OVTAScoreEquivalentCalculation:
                    return OVTAScoreEquivalentCalculation;
                case FieldDefinitionTypeEnum.Parcel:
                    return Parcel;
                case FieldDefinitionTypeEnum.Password:
                    return Password;
                case FieldDefinitionTypeEnum.PercentCaptured:
                    return PercentCaptured;
                case FieldDefinitionTypeEnum.PercentOfSiteTreated:
                    return PercentOfSiteTreated;
                case FieldDefinitionTypeEnum.PercentRetained:
                    return PercentRetained;
                case FieldDefinitionTypeEnum.PermanentPoolOrWetlandVolume:
                    return PermanentPoolOrWetlandVolume;
                case FieldDefinitionTypeEnum.PhotoCaption:
                    return PhotoCaption;
                case FieldDefinitionTypeEnum.PhotoCredit:
                    return PhotoCredit;
                case FieldDefinitionTypeEnum.PhotoTiming:
                    return PhotoTiming;
                case FieldDefinitionTypeEnum.PollutantContributiontoSEA:
                    return PollutantContributiontoSEA;
                case FieldDefinitionTypeEnum.PrimaryContact:
                    return PrimaryContact;
                case FieldDefinitionTypeEnum.ProgressScore:
                    return ProgressScore;
                case FieldDefinitionTypeEnum.PropertiesToObserve:
                    return PropertiesToObserve;
                case FieldDefinitionTypeEnum.ReceivesSystemCommunications:
                    return ReceivesSystemCommunications;
                case FieldDefinitionTypeEnum.ReceivingWaterScore:
                    return ReceivingWaterScore;
                case FieldDefinitionTypeEnum.RegionalSubbasin:
                    return RegionalSubbasin;
                case FieldDefinitionTypeEnum.RequiredFieldVisitsPerYear:
                    return RequiredFieldVisitsPerYear;
                case FieldDefinitionTypeEnum.RequiredLifespanOfInstallation:
                    return RequiredLifespanOfInstallation;
                case FieldDefinitionTypeEnum.RequiredPostStormFieldVisitsPerYear:
                    return RequiredPostStormFieldVisitsPerYear;
                case FieldDefinitionTypeEnum.RetainedOrRecycled:
                    return RetainedOrRecycled;
                case FieldDefinitionTypeEnum.RoleName:
                    return RoleName;
                case FieldDefinitionTypeEnum.RoutingConfiguration:
                    return RoutingConfiguration;
                case FieldDefinitionTypeEnum.SEAScore:
                    return SEAScore;
                case FieldDefinitionTypeEnum.SiteRunoff:
                    return SiteRunoff;
                case FieldDefinitionTypeEnum.SizingBasis:
                    return SizingBasis;
                case FieldDefinitionTypeEnum.StorageVolumeBelowLowestOutletElevation:
                    return StorageVolumeBelowLowestOutletElevation;
                case FieldDefinitionTypeEnum.SummerHarvestedWaterDemand:
                    return SummerHarvestedWaterDemand;
                case FieldDefinitionTypeEnum.TargetLoadReduction:
                    return TargetLoadReduction;
                case FieldDefinitionTypeEnum.TimeOfConcentrationID:
                    return TimeOfConcentrationID;
                case FieldDefinitionTypeEnum.TotalAchieved:
                    return TotalAchieved;
                case FieldDefinitionTypeEnum.TotalCopper:
                    return TotalCopper;
                case FieldDefinitionTypeEnum.TotalEffectiveBMPVolume:
                    return TotalEffectiveBMPVolume;
                case FieldDefinitionTypeEnum.TotalEffectiveDrywellBMPVolume:
                    return TotalEffectiveDrywellBMPVolume;
                case FieldDefinitionTypeEnum.TotalLead:
                    return TotalLead;
                case FieldDefinitionTypeEnum.TotalNitrogen:
                    return TotalNitrogen;
                case FieldDefinitionTypeEnum.TotalPhosphorous:
                    return TotalPhosphorous;
                case FieldDefinitionTypeEnum.TotalSuspendedSolids:
                    return TotalSuspendedSolids;
                case FieldDefinitionTypeEnum.TotalZinc:
                    return TotalZinc;
                case FieldDefinitionTypeEnum.TPIScore:
                    return TPIScore;
                case FieldDefinitionTypeEnum.TrashCaptureStatus:
                    return TrashCaptureStatus;
                case FieldDefinitionTypeEnum.TrashGenerationRate:
                    return TrashGenerationRate;
                case FieldDefinitionTypeEnum.TreatedAndDischarged:
                    return TreatedAndDischarged;
                case FieldDefinitionTypeEnum.TreatmentBMP:
                    return TreatmentBMP;
                case FieldDefinitionTypeEnum.TreatmentBMPAssessmentObservationType:
                    return TreatmentBMPAssessmentObservationType;
                case FieldDefinitionTypeEnum.TreatmentBMPDesignDepth:
                    return TreatmentBMPDesignDepth;
                case FieldDefinitionTypeEnum.TreatmentBMPType:
                    return TreatmentBMPType;
                case FieldDefinitionTypeEnum.TreatmentRate:
                    return TreatmentRate;
                case FieldDefinitionTypeEnum.UnderlyingHydrologicSoilGroupID:
                    return UnderlyingHydrologicSoilGroupID;
                case FieldDefinitionTypeEnum.UnderlyingInfiltrationRate:
                    return UnderlyingInfiltrationRate;
                case FieldDefinitionTypeEnum.UntreatedBypassOrOverflow:
                    return UntreatedBypassOrOverflow;
                case FieldDefinitionTypeEnum.UpstreamBMP:
                    return UpstreamBMP;
                case FieldDefinitionTypeEnum.Username:
                    return Username;
                case FieldDefinitionTypeEnum.ViaFullCapture:
                    return ViaFullCapture;
                case FieldDefinitionTypeEnum.ViaOVTAScore:
                    return ViaOVTAScore;
                case FieldDefinitionTypeEnum.ViaPartialCapture:
                    return ViaPartialCapture;
                case FieldDefinitionTypeEnum.WaterQualityManagementPlan:
                    return WaterQualityManagementPlan;
                case FieldDefinitionTypeEnum.WaterQualityManagementPlanDocumentType:
                    return WaterQualityManagementPlanDocumentType;
                case FieldDefinitionTypeEnum.Watershed:
                    return Watershed;
                case FieldDefinitionTypeEnum.WettedFootprint:
                    return WettedFootprint;
                case FieldDefinitionTypeEnum.WinterHarvestedWaterDemand:
                    return WinterHarvestedWaterDemand;
                case FieldDefinitionTypeEnum.WQLRI:
                    return WQLRI;
                case FieldDefinitionTypeEnum.WQMPTrashTreatmentStatus:
                    return WQMPTrashTreatmentStatus;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum FieldDefinitionTypeEnum
    {
        IsPrimaryContactOrganization = 1,
        Organization = 2,
        Password = 3,
        MeasurementUnit = 4,
        PhotoCaption = 5,
        PhotoCredit = 6,
        PhotoTiming = 7,
        PrimaryContact = 8,
        OrganizationType = 9,
        Username = 10,
        ExternalLinks = 11,
        RoleName = 12,
        ChartLastUpdatedDate = 13,
        TreatmentBMPType = 14,
        ConveyanceFunctionsAsIntended = 16,
        AssessmentScoreWeight = 17,
        ObservationScore = 18,
        AlternativeScore = 19,
        AssessmentForInternalUseOnly = 20,
        TreatmentBMPDesignDepth = 21,
        ReceivesSystemCommunications = 22,
        Jurisdiction = 23,
        Delineation = 24,
        TreatmentBMP = 25,
        TreatmentBMPAssessmentObservationType = 26,
        ObservationCollectionMethod = 27,
        ObservationThresholdType = 28,
        ObservationTargetType = 29,
        MeasurementUnitLabel = 30,
        PropertiesToObserve = 31,
        MinimumNumberOfObservations = 32,
        MaximumNumberOfObservations = 33,
        MinimumValueOfEachObservation = 34,
        MaximumValueOfEachObservation = 35,
        DefaultThresholdValue = 36,
        DefaultBenchmarkValue = 37,
        AssessmentFailsIfObservationFails = 38,
        CustomAttributeType = 39,
        CustomAttributeDataType = 40,
        MaintenanceRecordType = 41,
        MaintenanceRecord = 42,
        AttributeTypePurpose = 43,
        FundingSource = 44,
        IsPostMaintenanceAssessment = 45,
        FundingEvent = 46,
        FieldVisit = 47,
        FieldVisitStatus = 48,
        WaterQualityManagementPlan = 49,
        Parcel = 50,
        RequiredLifespanOfInstallation = 51,
        RequiredFieldVisitsPerYear = 52,
        RequiredPostStormFieldVisitsPerYear = 53,
        WaterQualityManagementPlanDocumentType = 54,
        HasAllRequiredDocuments = 55,
        DateOfLastInventoryChange = 56,
        TrashCaptureStatus = 57,
        OnlandVisualTrashAssessment = 58,
        OnlandVisualTrashAssessmentNotes = 59,
        DelineationType = 60,
        BaselineScore = 61,
        SizingBasis = 62,
        ProgressScore = 63,
        AssessmentScore = 64,
        ViaFullCapture = 65,
        ViaPartialCapture = 66,
        ViaOVTAScore = 67,
        TotalAchieved = 68,
        TargetLoadReduction = 69,
        TrashGenerationRate = 70,
        LandUse = 71,
        Area = 72,
        ImperviousArea = 73,
        GrossArea = 74,
        LandUseStatistics = 75,
        RegionalSubbasin = 76,
        AverageDivertedFlowrate = 77,
        AverageTreatmentFlowrate = 78,
        DesignDryWeatherTreatmentCapacity = 79,
        DesignLowFlowDiversionCapacity = 80,
        DesignMediaFiltrationRate = 81,
        DesignResidenceTimeForPermanentPool = 82,
        DiversionRate = 83,
        DrawdownTimeForWQDetentionVolume = 84,
        EffectiveFootprint = 85,
        EffectiveRetentionDepth = 86,
        InfiltrationDischargeRate = 87,
        InfiltrationSurfaceArea = 88,
        MediaBedFootprint = 89,
        MonthsOfOperationID = 90,
        PermanentPoolOrWetlandVolume = 91,
        RoutingConfiguration = 92,
        StorageVolumeBelowLowestOutletElevation = 93,
        SummerHarvestedWaterDemand = 94,
        TimeOfConcentrationID = 95,
        DrawdownTimeForDetentionVolume = 96,
        TotalEffectiveBMPVolume = 97,
        TotalEffectiveDrywellBMPVolume = 98,
        TreatmentRate = 99,
        UnderlyingHydrologicSoilGroupID = 100,
        UnderlyingInfiltrationRate = 101,
        UpstreamBMP = 102,
        ExtendedDetentionSurchargeVolume = 103,
        WettedFootprint = 104,
        WinterHarvestedWaterDemand = 105,
        PercentOfSiteTreated = 106,
        PercentCaptured = 107,
        PercentRetained = 108,
        AreaWithinWQMP = 109,
        Watershed = 110,
        DesignStormwaterDepth = 111,
        FullyParameterized = 112,
        HydromodificationApplies = 113,
        DelineationStatus = 114,
        DryWeatherFlowOverrideID = 115,
        ModeledPerformance = 116,
        OCTAM2Tier2GrantProgram = 117,
        SEAScore = 118,
        TPIScore = 119,
        WQLRI = 120,
        PollutantContributiontoSEA = 121,
        SiteRunoff = 122,
        TreatedAndDischarged = 123,
        RetainedOrRecycled = 124,
        UntreatedBypassOrOverflow = 125,
        TotalSuspendedSolids = 126,
        TotalNitrogen = 127,
        TotalPhosphorous = 128,
        FecalColiform = 129,
        TotalCopper = 130,
        TotalLead = 131,
        TotalZinc = 132,
        OCTAWatershed = 133,
        EffectiveAreaAcres = 134,
        DesignStormDepth85thPercentile = 135,
        DesignVolume85thPercentile = 136,
        LandUseBasedWaterQualityScore = 137,
        ReceivingWaterScore = 138,
        NumberOfIndividualBMPs = 139,
        LandUseType = 140,
        LandUseDescription = 141,
        OVTAScoreEquivalentCalculation = 142,
        BaselineLoadingRate = 143,
        CurrentLoadingRate = 144,
        BMPTrashTreatmentStatus = 145,
        WQMPTrashTreatmentStatus = 146,
        DownstreamOfNonModeledBMP = 147
    }

    public partial class FieldDefinitionTypeIsPrimaryContactOrganization : FieldDefinitionType
    {
        private FieldDefinitionTypeIsPrimaryContactOrganization(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeIsPrimaryContactOrganization Instance = new FieldDefinitionTypeIsPrimaryContactOrganization(1, @"IsPrimaryContactOrganization", @"Is Primary Contact Organization");
    }

    public partial class FieldDefinitionTypeOrganization : FieldDefinitionType
    {
        private FieldDefinitionTypeOrganization(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeOrganization Instance = new FieldDefinitionTypeOrganization(2, @"Organization", @"Organization");
    }

    public partial class FieldDefinitionTypePassword : FieldDefinitionType
    {
        private FieldDefinitionTypePassword(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypePassword Instance = new FieldDefinitionTypePassword(3, @"Password", @"Password");
    }

    public partial class FieldDefinitionTypeMeasurementUnit : FieldDefinitionType
    {
        private FieldDefinitionTypeMeasurementUnit(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeMeasurementUnit Instance = new FieldDefinitionTypeMeasurementUnit(4, @"MeasurementUnit", @"Measurement Unit");
    }

    public partial class FieldDefinitionTypePhotoCaption : FieldDefinitionType
    {
        private FieldDefinitionTypePhotoCaption(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypePhotoCaption Instance = new FieldDefinitionTypePhotoCaption(5, @"PhotoCaption", @"Photo Caption");
    }

    public partial class FieldDefinitionTypePhotoCredit : FieldDefinitionType
    {
        private FieldDefinitionTypePhotoCredit(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypePhotoCredit Instance = new FieldDefinitionTypePhotoCredit(6, @"PhotoCredit", @"Photo Credit");
    }

    public partial class FieldDefinitionTypePhotoTiming : FieldDefinitionType
    {
        private FieldDefinitionTypePhotoTiming(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypePhotoTiming Instance = new FieldDefinitionTypePhotoTiming(7, @"PhotoTiming", @"Photo Timing");
    }

    public partial class FieldDefinitionTypePrimaryContact : FieldDefinitionType
    {
        private FieldDefinitionTypePrimaryContact(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypePrimaryContact Instance = new FieldDefinitionTypePrimaryContact(8, @"PrimaryContact", @"Primary Contact");
    }

    public partial class FieldDefinitionTypeOrganizationType : FieldDefinitionType
    {
        private FieldDefinitionTypeOrganizationType(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeOrganizationType Instance = new FieldDefinitionTypeOrganizationType(9, @"OrganizationType", @"Organization Type");
    }

    public partial class FieldDefinitionTypeUsername : FieldDefinitionType
    {
        private FieldDefinitionTypeUsername(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeUsername Instance = new FieldDefinitionTypeUsername(10, @"Username", @"User name");
    }

    public partial class FieldDefinitionTypeExternalLinks : FieldDefinitionType
    {
        private FieldDefinitionTypeExternalLinks(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeExternalLinks Instance = new FieldDefinitionTypeExternalLinks(11, @"ExternalLinks", @"External Links");
    }

    public partial class FieldDefinitionTypeRoleName : FieldDefinitionType
    {
        private FieldDefinitionTypeRoleName(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeRoleName Instance = new FieldDefinitionTypeRoleName(12, @"RoleName", @"Role Name");
    }

    public partial class FieldDefinitionTypeChartLastUpdatedDate : FieldDefinitionType
    {
        private FieldDefinitionTypeChartLastUpdatedDate(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeChartLastUpdatedDate Instance = new FieldDefinitionTypeChartLastUpdatedDate(13, @"Chart Last Updated Date", @"Chart Last Updated Date");
    }

    public partial class FieldDefinitionTypeTreatmentBMPType : FieldDefinitionType
    {
        private FieldDefinitionTypeTreatmentBMPType(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTreatmentBMPType Instance = new FieldDefinitionTypeTreatmentBMPType(14, @"TreatmentBMPType", @"Treatment BMP Type");
    }

    public partial class FieldDefinitionTypeConveyanceFunctionsAsIntended : FieldDefinitionType
    {
        private FieldDefinitionTypeConveyanceFunctionsAsIntended(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeConveyanceFunctionsAsIntended Instance = new FieldDefinitionTypeConveyanceFunctionsAsIntended(16, @"ConveyanceFunctionsAsIntended", @"Conveyance Functions as Intended");
    }

    public partial class FieldDefinitionTypeAssessmentScoreWeight : FieldDefinitionType
    {
        private FieldDefinitionTypeAssessmentScoreWeight(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeAssessmentScoreWeight Instance = new FieldDefinitionTypeAssessmentScoreWeight(17, @"AssessmentScoreWeight", @"Assessment Score Weight");
    }

    public partial class FieldDefinitionTypeObservationScore : FieldDefinitionType
    {
        private FieldDefinitionTypeObservationScore(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeObservationScore Instance = new FieldDefinitionTypeObservationScore(18, @"ObservationScore", @"Observation Score");
    }

    public partial class FieldDefinitionTypeAlternativeScore : FieldDefinitionType
    {
        private FieldDefinitionTypeAlternativeScore(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeAlternativeScore Instance = new FieldDefinitionTypeAlternativeScore(19, @"AlternativeScore", @"Alternative Score");
    }

    public partial class FieldDefinitionTypeAssessmentForInternalUseOnly : FieldDefinitionType
    {
        private FieldDefinitionTypeAssessmentForInternalUseOnly(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeAssessmentForInternalUseOnly Instance = new FieldDefinitionTypeAssessmentForInternalUseOnly(20, @"AssessmentForInternalUseOnly", @"Assessment for Internal Use Only");
    }

    public partial class FieldDefinitionTypeTreatmentBMPDesignDepth : FieldDefinitionType
    {
        private FieldDefinitionTypeTreatmentBMPDesignDepth(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTreatmentBMPDesignDepth Instance = new FieldDefinitionTypeTreatmentBMPDesignDepth(21, @"TreatmentBMPDesignDepth", @"Treatment BMP Design Depth");
    }

    public partial class FieldDefinitionTypeReceivesSystemCommunications : FieldDefinitionType
    {
        private FieldDefinitionTypeReceivesSystemCommunications(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeReceivesSystemCommunications Instance = new FieldDefinitionTypeReceivesSystemCommunications(22, @"ReceivesSystemCommunications", @"Receives System Communications");
    }

    public partial class FieldDefinitionTypeJurisdiction : FieldDefinitionType
    {
        private FieldDefinitionTypeJurisdiction(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeJurisdiction Instance = new FieldDefinitionTypeJurisdiction(23, @"Jurisdiction", @"Jurisdiction");
    }

    public partial class FieldDefinitionTypeDelineation : FieldDefinitionType
    {
        private FieldDefinitionTypeDelineation(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDelineation Instance = new FieldDefinitionTypeDelineation(24, @"Delineation", @"Delineation");
    }

    public partial class FieldDefinitionTypeTreatmentBMP : FieldDefinitionType
    {
        private FieldDefinitionTypeTreatmentBMP(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTreatmentBMP Instance = new FieldDefinitionTypeTreatmentBMP(25, @"TreatmentBMP", @"Treatment BMP");
    }

    public partial class FieldDefinitionTypeTreatmentBMPAssessmentObservationType : FieldDefinitionType
    {
        private FieldDefinitionTypeTreatmentBMPAssessmentObservationType(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTreatmentBMPAssessmentObservationType Instance = new FieldDefinitionTypeTreatmentBMPAssessmentObservationType(26, @"TreatmentBMPAssessmentObservationType", @"Observation Name");
    }

    public partial class FieldDefinitionTypeObservationCollectionMethod : FieldDefinitionType
    {
        private FieldDefinitionTypeObservationCollectionMethod(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeObservationCollectionMethod Instance = new FieldDefinitionTypeObservationCollectionMethod(27, @"ObservationCollectionMethod", @"Collection Method");
    }

    public partial class FieldDefinitionTypeObservationThresholdType : FieldDefinitionType
    {
        private FieldDefinitionTypeObservationThresholdType(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeObservationThresholdType Instance = new FieldDefinitionTypeObservationThresholdType(28, @"ObservationThresholdType", @"Threshold Type");
    }

    public partial class FieldDefinitionTypeObservationTargetType : FieldDefinitionType
    {
        private FieldDefinitionTypeObservationTargetType(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeObservationTargetType Instance = new FieldDefinitionTypeObservationTargetType(29, @"ObservationTargetType", @"Target Type");
    }

    public partial class FieldDefinitionTypeMeasurementUnitLabel : FieldDefinitionType
    {
        private FieldDefinitionTypeMeasurementUnitLabel(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeMeasurementUnitLabel Instance = new FieldDefinitionTypeMeasurementUnitLabel(30, @"MeasurementUnitLabel", @"Measurement Unit Label");
    }

    public partial class FieldDefinitionTypePropertiesToObserve : FieldDefinitionType
    {
        private FieldDefinitionTypePropertiesToObserve(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypePropertiesToObserve Instance = new FieldDefinitionTypePropertiesToObserve(31, @"PropertiesToObserve", @"Properties To Observe");
    }

    public partial class FieldDefinitionTypeMinimumNumberOfObservations : FieldDefinitionType
    {
        private FieldDefinitionTypeMinimumNumberOfObservations(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeMinimumNumberOfObservations Instance = new FieldDefinitionTypeMinimumNumberOfObservations(32, @"MinimumNumberOfObservations", @"Minimum Number of Observations");
    }

    public partial class FieldDefinitionTypeMaximumNumberOfObservations : FieldDefinitionType
    {
        private FieldDefinitionTypeMaximumNumberOfObservations(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeMaximumNumberOfObservations Instance = new FieldDefinitionTypeMaximumNumberOfObservations(33, @"MaximumNumberOfObservations", @"Maximum Number of Observations");
    }

    public partial class FieldDefinitionTypeMinimumValueOfEachObservation : FieldDefinitionType
    {
        private FieldDefinitionTypeMinimumValueOfEachObservation(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeMinimumValueOfEachObservation Instance = new FieldDefinitionTypeMinimumValueOfEachObservation(34, @"MinimumValueOfEachObservation", @"Minimum Value of Each Observation");
    }

    public partial class FieldDefinitionTypeMaximumValueOfEachObservation : FieldDefinitionType
    {
        private FieldDefinitionTypeMaximumValueOfEachObservation(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeMaximumValueOfEachObservation Instance = new FieldDefinitionTypeMaximumValueOfEachObservation(35, @"MaximumValueOfEachObservation", @"Maximum Value of Each Observation");
    }

    public partial class FieldDefinitionTypeDefaultThresholdValue : FieldDefinitionType
    {
        private FieldDefinitionTypeDefaultThresholdValue(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDefaultThresholdValue Instance = new FieldDefinitionTypeDefaultThresholdValue(36, @"DefaultThresholdValue", @"Default Threshold Value");
    }

    public partial class FieldDefinitionTypeDefaultBenchmarkValue : FieldDefinitionType
    {
        private FieldDefinitionTypeDefaultBenchmarkValue(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDefaultBenchmarkValue Instance = new FieldDefinitionTypeDefaultBenchmarkValue(37, @"DefaultBenchmarkValue", @"Default Benchmark Value");
    }

    public partial class FieldDefinitionTypeAssessmentFailsIfObservationFails : FieldDefinitionType
    {
        private FieldDefinitionTypeAssessmentFailsIfObservationFails(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeAssessmentFailsIfObservationFails Instance = new FieldDefinitionTypeAssessmentFailsIfObservationFails(38, @"AssessmentFailsIfObservationFails", @"Assessment Fails if Observation Fails");
    }

    public partial class FieldDefinitionTypeCustomAttributeType : FieldDefinitionType
    {
        private FieldDefinitionTypeCustomAttributeType(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeCustomAttributeType Instance = new FieldDefinitionTypeCustomAttributeType(39, @"CustomAttributeType", @"Attribute Name");
    }

    public partial class FieldDefinitionTypeCustomAttributeDataType : FieldDefinitionType
    {
        private FieldDefinitionTypeCustomAttributeDataType(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeCustomAttributeDataType Instance = new FieldDefinitionTypeCustomAttributeDataType(40, @"CustomAttributeDataType", @"Data Type");
    }

    public partial class FieldDefinitionTypeMaintenanceRecordType : FieldDefinitionType
    {
        private FieldDefinitionTypeMaintenanceRecordType(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeMaintenanceRecordType Instance = new FieldDefinitionTypeMaintenanceRecordType(41, @"MaintenanceRecordType", @"Maintenance Type");
    }

    public partial class FieldDefinitionTypeMaintenanceRecord : FieldDefinitionType
    {
        private FieldDefinitionTypeMaintenanceRecord(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeMaintenanceRecord Instance = new FieldDefinitionTypeMaintenanceRecord(42, @"MaintenanceRecord", @"Maintenance Record");
    }

    public partial class FieldDefinitionTypeAttributeTypePurpose : FieldDefinitionType
    {
        private FieldDefinitionTypeAttributeTypePurpose(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeAttributeTypePurpose Instance = new FieldDefinitionTypeAttributeTypePurpose(43, @"AttributeTypePurpose", @"Purpose");
    }

    public partial class FieldDefinitionTypeFundingSource : FieldDefinitionType
    {
        private FieldDefinitionTypeFundingSource(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeFundingSource Instance = new FieldDefinitionTypeFundingSource(44, @"FundingSource", @"Funding Source");
    }

    public partial class FieldDefinitionTypeIsPostMaintenanceAssessment : FieldDefinitionType
    {
        private FieldDefinitionTypeIsPostMaintenanceAssessment(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeIsPostMaintenanceAssessment Instance = new FieldDefinitionTypeIsPostMaintenanceAssessment(45, @"IsPostMaintenanceAssessment", @"Post Maintenance Assessment?");
    }

    public partial class FieldDefinitionTypeFundingEvent : FieldDefinitionType
    {
        private FieldDefinitionTypeFundingEvent(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeFundingEvent Instance = new FieldDefinitionTypeFundingEvent(46, @"FundingEvent", @"Funding Event");
    }

    public partial class FieldDefinitionTypeFieldVisit : FieldDefinitionType
    {
        private FieldDefinitionTypeFieldVisit(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeFieldVisit Instance = new FieldDefinitionTypeFieldVisit(47, @"FieldVisit", @"Field Visit");
    }

    public partial class FieldDefinitionTypeFieldVisitStatus : FieldDefinitionType
    {
        private FieldDefinitionTypeFieldVisitStatus(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeFieldVisitStatus Instance = new FieldDefinitionTypeFieldVisitStatus(48, @"FieldVisitStatus", @"Field Visit Status");
    }

    public partial class FieldDefinitionTypeWaterQualityManagementPlan : FieldDefinitionType
    {
        private FieldDefinitionTypeWaterQualityManagementPlan(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeWaterQualityManagementPlan Instance = new FieldDefinitionTypeWaterQualityManagementPlan(49, @"WaterQualityManagementPlan", @"Water Quality Management Plan");
    }

    public partial class FieldDefinitionTypeParcel : FieldDefinitionType
    {
        private FieldDefinitionTypeParcel(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeParcel Instance = new FieldDefinitionTypeParcel(50, @"Parcel", @"Parcel");
    }

    public partial class FieldDefinitionTypeRequiredLifespanOfInstallation : FieldDefinitionType
    {
        private FieldDefinitionTypeRequiredLifespanOfInstallation(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeRequiredLifespanOfInstallation Instance = new FieldDefinitionTypeRequiredLifespanOfInstallation(51, @"RequiredLifespanOfInstallation", @"Required Lifespan of Installation");
    }

    public partial class FieldDefinitionTypeRequiredFieldVisitsPerYear : FieldDefinitionType
    {
        private FieldDefinitionTypeRequiredFieldVisitsPerYear(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeRequiredFieldVisitsPerYear Instance = new FieldDefinitionTypeRequiredFieldVisitsPerYear(52, @"RequiredFieldVisitsPerYear", @"Required Field Visits Per Year");
    }

    public partial class FieldDefinitionTypeRequiredPostStormFieldVisitsPerYear : FieldDefinitionType
    {
        private FieldDefinitionTypeRequiredPostStormFieldVisitsPerYear(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeRequiredPostStormFieldVisitsPerYear Instance = new FieldDefinitionTypeRequiredPostStormFieldVisitsPerYear(53, @"RequiredPostStormFieldVisitsPerYear", @"Required Post-Storm Field Visits Per Year");
    }

    public partial class FieldDefinitionTypeWaterQualityManagementPlanDocumentType : FieldDefinitionType
    {
        private FieldDefinitionTypeWaterQualityManagementPlanDocumentType(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeWaterQualityManagementPlanDocumentType Instance = new FieldDefinitionTypeWaterQualityManagementPlanDocumentType(54, @"WaterQualityManagementPlanDocumentType", @"WQMP Document Type");
    }

    public partial class FieldDefinitionTypeHasAllRequiredDocuments : FieldDefinitionType
    {
        private FieldDefinitionTypeHasAllRequiredDocuments(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeHasAllRequiredDocuments Instance = new FieldDefinitionTypeHasAllRequiredDocuments(55, @"HasAllRequiredDocuments", @"Has All Required Documents?");
    }

    public partial class FieldDefinitionTypeDateOfLastInventoryChange : FieldDefinitionType
    {
        private FieldDefinitionTypeDateOfLastInventoryChange(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDateOfLastInventoryChange Instance = new FieldDefinitionTypeDateOfLastInventoryChange(56, @"DateOfLastInventoryChange", @"Date of Last Inventory Change");
    }

    public partial class FieldDefinitionTypeTrashCaptureStatus : FieldDefinitionType
    {
        private FieldDefinitionTypeTrashCaptureStatus(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTrashCaptureStatus Instance = new FieldDefinitionTypeTrashCaptureStatus(57, @"TrashCaptureStatus", @"Trash Capture Status");
    }

    public partial class FieldDefinitionTypeOnlandVisualTrashAssessment : FieldDefinitionType
    {
        private FieldDefinitionTypeOnlandVisualTrashAssessment(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeOnlandVisualTrashAssessment Instance = new FieldDefinitionTypeOnlandVisualTrashAssessment(58, @"OnlandVisualTrashAssessment", @"On-land Visual Trash Assessment");
    }

    public partial class FieldDefinitionTypeOnlandVisualTrashAssessmentNotes : FieldDefinitionType
    {
        private FieldDefinitionTypeOnlandVisualTrashAssessmentNotes(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeOnlandVisualTrashAssessmentNotes Instance = new FieldDefinitionTypeOnlandVisualTrashAssessmentNotes(59, @"OnlandVisualTrashAssessmentNotes", @"Comments and Additional Information");
    }

    public partial class FieldDefinitionTypeDelineationType : FieldDefinitionType
    {
        private FieldDefinitionTypeDelineationType(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDelineationType Instance = new FieldDefinitionTypeDelineationType(60, @"DelineationType", @"Delineation Type");
    }

    public partial class FieldDefinitionTypeBaselineScore : FieldDefinitionType
    {
        private FieldDefinitionTypeBaselineScore(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeBaselineScore Instance = new FieldDefinitionTypeBaselineScore(61, @"BaselineScore", @"Baseline Score");
    }

    public partial class FieldDefinitionTypeSizingBasis : FieldDefinitionType
    {
        private FieldDefinitionTypeSizingBasis(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeSizingBasis Instance = new FieldDefinitionTypeSizingBasis(62, @"SizingBasis", @"Sizing Basis");
    }

    public partial class FieldDefinitionTypeProgressScore : FieldDefinitionType
    {
        private FieldDefinitionTypeProgressScore(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeProgressScore Instance = new FieldDefinitionTypeProgressScore(63, @"ProgressScore", @"Progress Score");
    }

    public partial class FieldDefinitionTypeAssessmentScore : FieldDefinitionType
    {
        private FieldDefinitionTypeAssessmentScore(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeAssessmentScore Instance = new FieldDefinitionTypeAssessmentScore(64, @"AssessmentScore", @"Assessment Score");
    }

    public partial class FieldDefinitionTypeViaFullCapture : FieldDefinitionType
    {
        private FieldDefinitionTypeViaFullCapture(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeViaFullCapture Instance = new FieldDefinitionTypeViaFullCapture(65, @"ViaFullCapture", @"Via Full Capture");
    }

    public partial class FieldDefinitionTypeViaPartialCapture : FieldDefinitionType
    {
        private FieldDefinitionTypeViaPartialCapture(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeViaPartialCapture Instance = new FieldDefinitionTypeViaPartialCapture(66, @"ViaPartialCapture", @"Via Partial Capture");
    }

    public partial class FieldDefinitionTypeViaOVTAScore : FieldDefinitionType
    {
        private FieldDefinitionTypeViaOVTAScore(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeViaOVTAScore Instance = new FieldDefinitionTypeViaOVTAScore(67, @"ViaOVTAScore", @"Via OVTA Score");
    }

    public partial class FieldDefinitionTypeTotalAchieved : FieldDefinitionType
    {
        private FieldDefinitionTypeTotalAchieved(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTotalAchieved Instance = new FieldDefinitionTypeTotalAchieved(68, @"TotalAchieved", @"Total Achieved");
    }

    public partial class FieldDefinitionTypeTargetLoadReduction : FieldDefinitionType
    {
        private FieldDefinitionTypeTargetLoadReduction(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTargetLoadReduction Instance = new FieldDefinitionTypeTargetLoadReduction(69, @"TargetLoadReduction", @"Target Load Reduction");
    }

    public partial class FieldDefinitionTypeTrashGenerationRate : FieldDefinitionType
    {
        private FieldDefinitionTypeTrashGenerationRate(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTrashGenerationRate Instance = new FieldDefinitionTypeTrashGenerationRate(70, @"TrashGenerationRate", @"Trash Generation Rate");
    }

    public partial class FieldDefinitionTypeLandUse : FieldDefinitionType
    {
        private FieldDefinitionTypeLandUse(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeLandUse Instance = new FieldDefinitionTypeLandUse(71, @"LandUse", @"Land Use");
    }

    public partial class FieldDefinitionTypeArea : FieldDefinitionType
    {
        private FieldDefinitionTypeArea(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeArea Instance = new FieldDefinitionTypeArea(72, @"Area", @"Area");
    }

    public partial class FieldDefinitionTypeImperviousArea : FieldDefinitionType
    {
        private FieldDefinitionTypeImperviousArea(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeImperviousArea Instance = new FieldDefinitionTypeImperviousArea(73, @"ImperviousArea", @"Impervious Area");
    }

    public partial class FieldDefinitionTypeGrossArea : FieldDefinitionType
    {
        private FieldDefinitionTypeGrossArea(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeGrossArea Instance = new FieldDefinitionTypeGrossArea(74, @"GrossArea", @"Gross Area");
    }

    public partial class FieldDefinitionTypeLandUseStatistics : FieldDefinitionType
    {
        private FieldDefinitionTypeLandUseStatistics(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeLandUseStatistics Instance = new FieldDefinitionTypeLandUseStatistics(75, @"LandUseStatistics", @"Land Use Statistics");
    }

    public partial class FieldDefinitionTypeRegionalSubbasin : FieldDefinitionType
    {
        private FieldDefinitionTypeRegionalSubbasin(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeRegionalSubbasin Instance = new FieldDefinitionTypeRegionalSubbasin(76, @"RegionalSubbasin", @"Regional Subbasin");
    }

    public partial class FieldDefinitionTypeAverageDivertedFlowrate : FieldDefinitionType
    {
        private FieldDefinitionTypeAverageDivertedFlowrate(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeAverageDivertedFlowrate Instance = new FieldDefinitionTypeAverageDivertedFlowrate(77, @"AverageDivertedFlowrate", @"Average Diverted Flowrate");
    }

    public partial class FieldDefinitionTypeAverageTreatmentFlowrate : FieldDefinitionType
    {
        private FieldDefinitionTypeAverageTreatmentFlowrate(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeAverageTreatmentFlowrate Instance = new FieldDefinitionTypeAverageTreatmentFlowrate(78, @"AverageTreatmentFlowrate", @"Average Treatment Flowrate");
    }

    public partial class FieldDefinitionTypeDesignDryWeatherTreatmentCapacity : FieldDefinitionType
    {
        private FieldDefinitionTypeDesignDryWeatherTreatmentCapacity(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDesignDryWeatherTreatmentCapacity Instance = new FieldDefinitionTypeDesignDryWeatherTreatmentCapacity(79, @"DesignDryWeatherTreatmentCapacity", @"Design Dry Weather Treatment Capacity");
    }

    public partial class FieldDefinitionTypeDesignLowFlowDiversionCapacity : FieldDefinitionType
    {
        private FieldDefinitionTypeDesignLowFlowDiversionCapacity(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDesignLowFlowDiversionCapacity Instance = new FieldDefinitionTypeDesignLowFlowDiversionCapacity(80, @"DesignLowFlowDiversionCapacity", @"Design Low Flow Diversion Capacity");
    }

    public partial class FieldDefinitionTypeDesignMediaFiltrationRate : FieldDefinitionType
    {
        private FieldDefinitionTypeDesignMediaFiltrationRate(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDesignMediaFiltrationRate Instance = new FieldDefinitionTypeDesignMediaFiltrationRate(81, @"DesignMediaFiltrationRate", @"Design Media Filtration Rate");
    }

    public partial class FieldDefinitionTypeDesignResidenceTimeForPermanentPool : FieldDefinitionType
    {
        private FieldDefinitionTypeDesignResidenceTimeForPermanentPool(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDesignResidenceTimeForPermanentPool Instance = new FieldDefinitionTypeDesignResidenceTimeForPermanentPool(82, @"DesignResidenceTimeForPermanentPool", @"Design Residence Time for Permanent Pool");
    }

    public partial class FieldDefinitionTypeDiversionRate : FieldDefinitionType
    {
        private FieldDefinitionTypeDiversionRate(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDiversionRate Instance = new FieldDefinitionTypeDiversionRate(83, @"DiversionRate", @"Diversion Rate");
    }

    public partial class FieldDefinitionTypeDrawdownTimeForWQDetentionVolume : FieldDefinitionType
    {
        private FieldDefinitionTypeDrawdownTimeForWQDetentionVolume(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDrawdownTimeForWQDetentionVolume Instance = new FieldDefinitionTypeDrawdownTimeForWQDetentionVolume(84, @"DrawdownTimeForWQDetentionVolume", @"Drawdown Time for WQ Detention Volume");
    }

    public partial class FieldDefinitionTypeEffectiveFootprint : FieldDefinitionType
    {
        private FieldDefinitionTypeEffectiveFootprint(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeEffectiveFootprint Instance = new FieldDefinitionTypeEffectiveFootprint(85, @"EffectiveFootprint", @"Effective Footprint");
    }

    public partial class FieldDefinitionTypeEffectiveRetentionDepth : FieldDefinitionType
    {
        private FieldDefinitionTypeEffectiveRetentionDepth(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeEffectiveRetentionDepth Instance = new FieldDefinitionTypeEffectiveRetentionDepth(86, @"EffectiveRetentionDepth", @"Effective Retention Depth");
    }

    public partial class FieldDefinitionTypeInfiltrationDischargeRate : FieldDefinitionType
    {
        private FieldDefinitionTypeInfiltrationDischargeRate(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeInfiltrationDischargeRate Instance = new FieldDefinitionTypeInfiltrationDischargeRate(87, @"InfiltrationDischargeRate", @"Infiltration Discharge Rate");
    }

    public partial class FieldDefinitionTypeInfiltrationSurfaceArea : FieldDefinitionType
    {
        private FieldDefinitionTypeInfiltrationSurfaceArea(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeInfiltrationSurfaceArea Instance = new FieldDefinitionTypeInfiltrationSurfaceArea(88, @"InfiltrationSurfaceArea", @"Infiltration Surface Area");
    }

    public partial class FieldDefinitionTypeMediaBedFootprint : FieldDefinitionType
    {
        private FieldDefinitionTypeMediaBedFootprint(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeMediaBedFootprint Instance = new FieldDefinitionTypeMediaBedFootprint(89, @"MediaBedFootprint", @"Media Bed Footprint");
    }

    public partial class FieldDefinitionTypeMonthsOfOperationID : FieldDefinitionType
    {
        private FieldDefinitionTypeMonthsOfOperationID(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeMonthsOfOperationID Instance = new FieldDefinitionTypeMonthsOfOperationID(90, @"MonthsOfOperationID", @"Months Operational");
    }

    public partial class FieldDefinitionTypePermanentPoolOrWetlandVolume : FieldDefinitionType
    {
        private FieldDefinitionTypePermanentPoolOrWetlandVolume(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypePermanentPoolOrWetlandVolume Instance = new FieldDefinitionTypePermanentPoolOrWetlandVolume(91, @"PermanentPoolOrWetlandVolume", @"Permanent Pool or Wetland Volume");
    }

    public partial class FieldDefinitionTypeRoutingConfiguration : FieldDefinitionType
    {
        private FieldDefinitionTypeRoutingConfiguration(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeRoutingConfiguration Instance = new FieldDefinitionTypeRoutingConfiguration(92, @"RoutingConfiguration", @"Routing Configuration");
    }

    public partial class FieldDefinitionTypeStorageVolumeBelowLowestOutletElevation : FieldDefinitionType
    {
        private FieldDefinitionTypeStorageVolumeBelowLowestOutletElevation(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeStorageVolumeBelowLowestOutletElevation Instance = new FieldDefinitionTypeStorageVolumeBelowLowestOutletElevation(93, @"StorageVolumeBelowLowestOutletElevation", @"Storage Volume Below Lowest Outlet Elevation");
    }

    public partial class FieldDefinitionTypeSummerHarvestedWaterDemand : FieldDefinitionType
    {
        private FieldDefinitionTypeSummerHarvestedWaterDemand(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeSummerHarvestedWaterDemand Instance = new FieldDefinitionTypeSummerHarvestedWaterDemand(94, @"SummerHarvestedWaterDemand", @"Summer Harvested Water Demand");
    }

    public partial class FieldDefinitionTypeTimeOfConcentrationID : FieldDefinitionType
    {
        private FieldDefinitionTypeTimeOfConcentrationID(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTimeOfConcentrationID Instance = new FieldDefinitionTypeTimeOfConcentrationID(95, @"TimeOfConcentrationID", @"Time of Concentration");
    }

    public partial class FieldDefinitionTypeDrawdownTimeForDetentionVolume : FieldDefinitionType
    {
        private FieldDefinitionTypeDrawdownTimeForDetentionVolume(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDrawdownTimeForDetentionVolume Instance = new FieldDefinitionTypeDrawdownTimeForDetentionVolume(96, @"DrawdownTimeForDetentionVolume", @"Drawdown Time For Detention Volume");
    }

    public partial class FieldDefinitionTypeTotalEffectiveBMPVolume : FieldDefinitionType
    {
        private FieldDefinitionTypeTotalEffectiveBMPVolume(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTotalEffectiveBMPVolume Instance = new FieldDefinitionTypeTotalEffectiveBMPVolume(97, @"TotalEffectiveBMPVolume", @"Total Effective BMP Volume");
    }

    public partial class FieldDefinitionTypeTotalEffectiveDrywellBMPVolume : FieldDefinitionType
    {
        private FieldDefinitionTypeTotalEffectiveDrywellBMPVolume(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTotalEffectiveDrywellBMPVolume Instance = new FieldDefinitionTypeTotalEffectiveDrywellBMPVolume(98, @"TotalEffectiveDrywellBMPVolume", @"Total Effective Drywell BMP Volume");
    }

    public partial class FieldDefinitionTypeTreatmentRate : FieldDefinitionType
    {
        private FieldDefinitionTypeTreatmentRate(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTreatmentRate Instance = new FieldDefinitionTypeTreatmentRate(99, @"TreatmentRate", @"Treatment Rate");
    }

    public partial class FieldDefinitionTypeUnderlyingHydrologicSoilGroupID : FieldDefinitionType
    {
        private FieldDefinitionTypeUnderlyingHydrologicSoilGroupID(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeUnderlyingHydrologicSoilGroupID Instance = new FieldDefinitionTypeUnderlyingHydrologicSoilGroupID(100, @"UnderlyingHydrologicSoilGroupID", @"Underlying Hydrologic Soil Group (HSG)");
    }

    public partial class FieldDefinitionTypeUnderlyingInfiltrationRate : FieldDefinitionType
    {
        private FieldDefinitionTypeUnderlyingInfiltrationRate(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeUnderlyingInfiltrationRate Instance = new FieldDefinitionTypeUnderlyingInfiltrationRate(101, @"UnderlyingInfiltrationRate", @"Underlying Infiltration Rate");
    }

    public partial class FieldDefinitionTypeUpstreamBMP : FieldDefinitionType
    {
        private FieldDefinitionTypeUpstreamBMP(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeUpstreamBMP Instance = new FieldDefinitionTypeUpstreamBMP(102, @"UpstreamBMP", @"Upstream BMP");
    }

    public partial class FieldDefinitionTypeExtendedDetentionSurchargeVolume : FieldDefinitionType
    {
        private FieldDefinitionTypeExtendedDetentionSurchargeVolume(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeExtendedDetentionSurchargeVolume Instance = new FieldDefinitionTypeExtendedDetentionSurchargeVolume(103, @"ExtendedDetentionSurchargeVolume", @"Extended Detention Surcharge Volume");
    }

    public partial class FieldDefinitionTypeWettedFootprint : FieldDefinitionType
    {
        private FieldDefinitionTypeWettedFootprint(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeWettedFootprint Instance = new FieldDefinitionTypeWettedFootprint(104, @"WettedFootprint", @"Wetted Footprint");
    }

    public partial class FieldDefinitionTypeWinterHarvestedWaterDemand : FieldDefinitionType
    {
        private FieldDefinitionTypeWinterHarvestedWaterDemand(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeWinterHarvestedWaterDemand Instance = new FieldDefinitionTypeWinterHarvestedWaterDemand(105, @"WinterHarvestedWaterDemand", @"Winter Harvested Water Demand");
    }

    public partial class FieldDefinitionTypePercentOfSiteTreated : FieldDefinitionType
    {
        private FieldDefinitionTypePercentOfSiteTreated(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypePercentOfSiteTreated Instance = new FieldDefinitionTypePercentOfSiteTreated(106, @"PercentOfSiteTreated", @"% of Site Treated");
    }

    public partial class FieldDefinitionTypePercentCaptured : FieldDefinitionType
    {
        private FieldDefinitionTypePercentCaptured(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypePercentCaptured Instance = new FieldDefinitionTypePercentCaptured(107, @"PercentCaptured", @"Wet Weather % Captured");
    }

    public partial class FieldDefinitionTypePercentRetained : FieldDefinitionType
    {
        private FieldDefinitionTypePercentRetained(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypePercentRetained Instance = new FieldDefinitionTypePercentRetained(108, @"PercentRetained", @"Wet Weather % Retained");
    }

    public partial class FieldDefinitionTypeAreaWithinWQMP : FieldDefinitionType
    {
        private FieldDefinitionTypeAreaWithinWQMP(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeAreaWithinWQMP Instance = new FieldDefinitionTypeAreaWithinWQMP(109, @"AreaWithinWQMP", @"Area within WQMP");
    }

    public partial class FieldDefinitionTypeWatershed : FieldDefinitionType
    {
        private FieldDefinitionTypeWatershed(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeWatershed Instance = new FieldDefinitionTypeWatershed(110, @"Watershed", @"Watershed");
    }

    public partial class FieldDefinitionTypeDesignStormwaterDepth : FieldDefinitionType
    {
        private FieldDefinitionTypeDesignStormwaterDepth(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDesignStormwaterDepth Instance = new FieldDefinitionTypeDesignStormwaterDepth(111, @"DesignStormwaterDepth", @"Design Stormwater Depth");
    }

    public partial class FieldDefinitionTypeFullyParameterized : FieldDefinitionType
    {
        private FieldDefinitionTypeFullyParameterized(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeFullyParameterized Instance = new FieldDefinitionTypeFullyParameterized(112, @"FullyParameterized", @"Fully Parameterized?");
    }

    public partial class FieldDefinitionTypeHydromodificationApplies : FieldDefinitionType
    {
        private FieldDefinitionTypeHydromodificationApplies(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeHydromodificationApplies Instance = new FieldDefinitionTypeHydromodificationApplies(113, @"HydromodificationApplies", @"Hydromodification Controls Apply");
    }

    public partial class FieldDefinitionTypeDelineationStatus : FieldDefinitionType
    {
        private FieldDefinitionTypeDelineationStatus(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDelineationStatus Instance = new FieldDefinitionTypeDelineationStatus(114, @"DelineationStatus", @"Delineation Status");
    }

    public partial class FieldDefinitionTypeDryWeatherFlowOverrideID : FieldDefinitionType
    {
        private FieldDefinitionTypeDryWeatherFlowOverrideID(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDryWeatherFlowOverrideID Instance = new FieldDefinitionTypeDryWeatherFlowOverrideID(115, @"DryWeatherFlowOverrideID", @"Dry Weather Flow Override?");
    }

    public partial class FieldDefinitionTypeModeledPerformance : FieldDefinitionType
    {
        private FieldDefinitionTypeModeledPerformance(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeModeledPerformance Instance = new FieldDefinitionTypeModeledPerformance(116, @"ModeledPerformance", @"Modeled Performance");
    }

    public partial class FieldDefinitionTypeOCTAM2Tier2GrantProgram : FieldDefinitionType
    {
        private FieldDefinitionTypeOCTAM2Tier2GrantProgram(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeOCTAM2Tier2GrantProgram Instance = new FieldDefinitionTypeOCTAM2Tier2GrantProgram(117, @"OCTA M2 Tier 2 Grant Program", @"OCTA M2 Tier 2 Grant Program");
    }

    public partial class FieldDefinitionTypeSEAScore : FieldDefinitionType
    {
        private FieldDefinitionTypeSEAScore(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeSEAScore Instance = new FieldDefinitionTypeSEAScore(118, @"SEA Score", @"Strategically Effective Area Score");
    }

    public partial class FieldDefinitionTypeTPIScore : FieldDefinitionType
    {
        private FieldDefinitionTypeTPIScore(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTPIScore Instance = new FieldDefinitionTypeTPIScore(119, @"TPI Score", @"Transportation Nexus Score");
    }

    public partial class FieldDefinitionTypeWQLRI : FieldDefinitionType
    {
        private FieldDefinitionTypeWQLRI(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeWQLRI Instance = new FieldDefinitionTypeWQLRI(120, @"WQLRI", @"Water Quality Load Reduction Index");
    }

    public partial class FieldDefinitionTypePollutantContributiontoSEA : FieldDefinitionType
    {
        private FieldDefinitionTypePollutantContributiontoSEA(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypePollutantContributiontoSEA Instance = new FieldDefinitionTypePollutantContributiontoSEA(121, @"Pollutant Contribution to SEA", @"Pollutant Contribution to Strategically Effective Area");
    }

    public partial class FieldDefinitionTypeSiteRunoff : FieldDefinitionType
    {
        private FieldDefinitionTypeSiteRunoff(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeSiteRunoff Instance = new FieldDefinitionTypeSiteRunoff(122, @"SiteRunoff", @"Site Runoff");
    }

    public partial class FieldDefinitionTypeTreatedAndDischarged : FieldDefinitionType
    {
        private FieldDefinitionTypeTreatedAndDischarged(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTreatedAndDischarged Instance = new FieldDefinitionTypeTreatedAndDischarged(123, @"TreatedAndDischarged", @"Treated and Discharged");
    }

    public partial class FieldDefinitionTypeRetainedOrRecycled : FieldDefinitionType
    {
        private FieldDefinitionTypeRetainedOrRecycled(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeRetainedOrRecycled Instance = new FieldDefinitionTypeRetainedOrRecycled(124, @"RetainedOrRecycled", @"Retained or Recycled");
    }

    public partial class FieldDefinitionTypeUntreatedBypassOrOverflow : FieldDefinitionType
    {
        private FieldDefinitionTypeUntreatedBypassOrOverflow(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeUntreatedBypassOrOverflow Instance = new FieldDefinitionTypeUntreatedBypassOrOverflow(125, @"Untreated(BypassOrOverflow)", @"Untreated (Bypass or Overflow)");
    }

    public partial class FieldDefinitionTypeTotalSuspendedSolids : FieldDefinitionType
    {
        private FieldDefinitionTypeTotalSuspendedSolids(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTotalSuspendedSolids Instance = new FieldDefinitionTypeTotalSuspendedSolids(126, @"TotalSuspendedSolids", @"Total Suspended Solids");
    }

    public partial class FieldDefinitionTypeTotalNitrogen : FieldDefinitionType
    {
        private FieldDefinitionTypeTotalNitrogen(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTotalNitrogen Instance = new FieldDefinitionTypeTotalNitrogen(127, @"TotalNitrogen", @"Total Nitrogen");
    }

    public partial class FieldDefinitionTypeTotalPhosphorous : FieldDefinitionType
    {
        private FieldDefinitionTypeTotalPhosphorous(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTotalPhosphorous Instance = new FieldDefinitionTypeTotalPhosphorous(128, @"TotalPhosphorous", @"Total Phosphorous");
    }

    public partial class FieldDefinitionTypeFecalColiform : FieldDefinitionType
    {
        private FieldDefinitionTypeFecalColiform(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeFecalColiform Instance = new FieldDefinitionTypeFecalColiform(129, @"FecalColiform", @"Fecal Coliform");
    }

    public partial class FieldDefinitionTypeTotalCopper : FieldDefinitionType
    {
        private FieldDefinitionTypeTotalCopper(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTotalCopper Instance = new FieldDefinitionTypeTotalCopper(130, @"TotalCopper", @"Total Copper");
    }

    public partial class FieldDefinitionTypeTotalLead : FieldDefinitionType
    {
        private FieldDefinitionTypeTotalLead(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTotalLead Instance = new FieldDefinitionTypeTotalLead(131, @"TotalLead", @"Total Lead");
    }

    public partial class FieldDefinitionTypeTotalZinc : FieldDefinitionType
    {
        private FieldDefinitionTypeTotalZinc(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeTotalZinc Instance = new FieldDefinitionTypeTotalZinc(132, @"TotalZinc", @"Total Zinc");
    }

    public partial class FieldDefinitionTypeOCTAWatershed : FieldDefinitionType
    {
        private FieldDefinitionTypeOCTAWatershed(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeOCTAWatershed Instance = new FieldDefinitionTypeOCTAWatershed(133, @"OCTAWatershed", @"OCTA Watershed");
    }

    public partial class FieldDefinitionTypeEffectiveAreaAcres : FieldDefinitionType
    {
        private FieldDefinitionTypeEffectiveAreaAcres(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeEffectiveAreaAcres Instance = new FieldDefinitionTypeEffectiveAreaAcres(134, @"EffectiveAreaAcres", @"Effective Area (acres)");
    }

    public partial class FieldDefinitionTypeDesignStormDepth85thPercentile : FieldDefinitionType
    {
        private FieldDefinitionTypeDesignStormDepth85thPercentile(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDesignStormDepth85thPercentile Instance = new FieldDefinitionTypeDesignStormDepth85thPercentile(135, @"DesignStormDepth85thPercentile", @"85th Percentile Design Storm Depth (inches)");
    }

    public partial class FieldDefinitionTypeDesignVolume85thPercentile : FieldDefinitionType
    {
        private FieldDefinitionTypeDesignVolume85thPercentile(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDesignVolume85thPercentile Instance = new FieldDefinitionTypeDesignVolume85thPercentile(136, @"DesignVolume85thPercentile", @"85th Percentile Design Volume (cuft)");
    }

    public partial class FieldDefinitionTypeLandUseBasedWaterQualityScore : FieldDefinitionType
    {
        private FieldDefinitionTypeLandUseBasedWaterQualityScore(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeLandUseBasedWaterQualityScore Instance = new FieldDefinitionTypeLandUseBasedWaterQualityScore(137, @"LandUseBasedWaterQualityScore", @"Land Use Based Water Quality Score");
    }

    public partial class FieldDefinitionTypeReceivingWaterScore : FieldDefinitionType
    {
        private FieldDefinitionTypeReceivingWaterScore(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeReceivingWaterScore Instance = new FieldDefinitionTypeReceivingWaterScore(138, @"ReceivingWaterScore", @"Receiving Water Score");
    }

    public partial class FieldDefinitionTypeNumberOfIndividualBMPs : FieldDefinitionType
    {
        private FieldDefinitionTypeNumberOfIndividualBMPs(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeNumberOfIndividualBMPs Instance = new FieldDefinitionTypeNumberOfIndividualBMPs(139, @"NumberOfIndividualBMPs", @"# of Individual BMPs");
    }

    public partial class FieldDefinitionTypeLandUseType : FieldDefinitionType
    {
        private FieldDefinitionTypeLandUseType(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeLandUseType Instance = new FieldDefinitionTypeLandUseType(140, @"LandUseType", @"Land Use Type");
    }

    public partial class FieldDefinitionTypeLandUseDescription : FieldDefinitionType
    {
        private FieldDefinitionTypeLandUseDescription(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeLandUseDescription Instance = new FieldDefinitionTypeLandUseDescription(141, @"LandUseDescription", @"Land Use Description");
    }

    public partial class FieldDefinitionTypeOVTAScoreEquivalentCalculation : FieldDefinitionType
    {
        private FieldDefinitionTypeOVTAScoreEquivalentCalculation(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeOVTAScoreEquivalentCalculation Instance = new FieldDefinitionTypeOVTAScoreEquivalentCalculation(142, @"OVTAScoreEquivalentCalculation", @"OVTA Score Equivalent Calculation");
    }

    public partial class FieldDefinitionTypeBaselineLoadingRate : FieldDefinitionType
    {
        private FieldDefinitionTypeBaselineLoadingRate(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeBaselineLoadingRate Instance = new FieldDefinitionTypeBaselineLoadingRate(143, @"BaselineLoadingRate", @"Baseline Trash Generation Rate");
    }

    public partial class FieldDefinitionTypeCurrentLoadingRate : FieldDefinitionType
    {
        private FieldDefinitionTypeCurrentLoadingRate(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeCurrentLoadingRate Instance = new FieldDefinitionTypeCurrentLoadingRate(144, @"CurrentLoadingRate", @"Current Net Loading Rate");
    }

    public partial class FieldDefinitionTypeBMPTrashTreatmentStatus : FieldDefinitionType
    {
        private FieldDefinitionTypeBMPTrashTreatmentStatus(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeBMPTrashTreatmentStatus Instance = new FieldDefinitionTypeBMPTrashTreatmentStatus(145, @"BMPTrashTreatmentStatus", @"BMP Trash Treatment Status");
    }

    public partial class FieldDefinitionTypeWQMPTrashTreatmentStatus : FieldDefinitionType
    {
        private FieldDefinitionTypeWQMPTrashTreatmentStatus(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeWQMPTrashTreatmentStatus Instance = new FieldDefinitionTypeWQMPTrashTreatmentStatus(146, @"WQMPTrashTreatmentStatus", @"WQMP Trash Treatment Status");
    }

    public partial class FieldDefinitionTypeDownstreamOfNonModeledBMP : FieldDefinitionType
    {
        private FieldDefinitionTypeDownstreamOfNonModeledBMP(int fieldDefinitionTypeID, string fieldDefinitionTypeName, string fieldDefinitionTypeDisplayName) : base(fieldDefinitionTypeID, fieldDefinitionTypeName, fieldDefinitionTypeDisplayName) {}
        public static readonly FieldDefinitionTypeDownstreamOfNonModeledBMP Instance = new FieldDefinitionTypeDownstreamOfNonModeledBMP(147, @"DownstreamOfNonModeledBMP", @"Downstream of Non-Modeled BMP?");
    }
}