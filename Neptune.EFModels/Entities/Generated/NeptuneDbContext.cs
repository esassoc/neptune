using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

public partial class NeptuneDbContext : DbContext
{
    public NeptuneDbContext(DbContextOptions<NeptuneDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AITokenUsage> AITokenUsages { get; set; }

    public virtual DbSet<County> Counties { get; set; }

    public virtual DbSet<CustomAttribute> CustomAttributes { get; set; }

    public virtual DbSet<CustomAttributeType> CustomAttributeTypes { get; set; }

    public virtual DbSet<CustomAttributeValue> CustomAttributeValues { get; set; }

    public virtual DbSet<Delineation> Delineations { get; set; }

    public virtual DbSet<DelineationOverlap> DelineationOverlaps { get; set; }

    public virtual DbSet<DelineationStaging> DelineationStagings { get; set; }

    public virtual DbSet<DirtyModelNode> DirtyModelNodes { get; set; }

    public virtual DbSet<FieldDefinition> FieldDefinitions { get; set; }

    public virtual DbSet<FieldVisit> FieldVisits { get; set; }

    public virtual DbSet<FileResource> FileResources { get; set; }

    public virtual DbSet<FundingEvent> FundingEvents { get; set; }

    public virtual DbSet<FundingEventFundingSource> FundingEventFundingSources { get; set; }

    public virtual DbSet<FundingSource> FundingSources { get; set; }

    public virtual DbSet<HRUCharacteristic> HRUCharacteristics { get; set; }

    public virtual DbSet<HRULog> HRULogs { get; set; }

    public virtual DbSet<HydrologicSubarea> HydrologicSubareas { get; set; }

    public virtual DbSet<LandUseBlock> LandUseBlocks { get; set; }

    public virtual DbSet<LandUseBlockStaging> LandUseBlockStagings { get; set; }

    public virtual DbSet<LoadGeneratingUnit> LoadGeneratingUnits { get; set; }

    public virtual DbSet<LoadGeneratingUnitRefreshArea> LoadGeneratingUnitRefreshAreas { get; set; }

    public virtual DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

    public virtual DbSet<MaintenanceRecordObservation> MaintenanceRecordObservations { get; set; }

    public virtual DbSet<MaintenanceRecordObservationValue> MaintenanceRecordObservationValues { get; set; }

    public virtual DbSet<ModelBasin> ModelBasins { get; set; }

    public virtual DbSet<ModelBasinStaging> ModelBasinStagings { get; set; }

    public virtual DbSet<NeptuneHomePageImage> NeptuneHomePageImages { get; set; }

    public virtual DbSet<NeptunePage> NeptunePages { get; set; }

    public virtual DbSet<NeptunePageImage> NeptunePageImages { get; set; }

    public virtual DbSet<NereidLog> NereidLogs { get; set; }

    public virtual DbSet<NereidResult> NereidResults { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OCTAPrioritization> OCTAPrioritizations { get; set; }

    public virtual DbSet<OCTAPrioritizationStaging> OCTAPrioritizationStagings { get; set; }

    public virtual DbSet<OnlandVisualTrashAssessment> OnlandVisualTrashAssessments { get; set; }

    public virtual DbSet<OnlandVisualTrashAssessmentArea> OnlandVisualTrashAssessmentAreas { get; set; }

    public virtual DbSet<OnlandVisualTrashAssessmentAreaStaging> OnlandVisualTrashAssessmentAreaStagings { get; set; }

    public virtual DbSet<OnlandVisualTrashAssessmentObservation> OnlandVisualTrashAssessmentObservations { get; set; }

    public virtual DbSet<OnlandVisualTrashAssessmentObservationPhoto> OnlandVisualTrashAssessmentObservationPhotos { get; set; }

    public virtual DbSet<OnlandVisualTrashAssessmentObservationPhotoStaging> OnlandVisualTrashAssessmentObservationPhotoStagings { get; set; }

    public virtual DbSet<OnlandVisualTrashAssessmentPreliminarySourceIdentificationType> OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<OrganizationType> OrganizationTypes { get; set; }

    public virtual DbSet<Parcel> Parcels { get; set; }

    public virtual DbSet<ParcelGeometry> ParcelGeometries { get; set; }

    public virtual DbSet<ParcelStaging> ParcelStagings { get; set; }

    public virtual DbSet<Person> People { get; set; }

    public virtual DbSet<PrecipitationZone> PrecipitationZones { get; set; }

    public virtual DbSet<PrecipitationZoneStaging> PrecipitationZoneStagings { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectDocument> ProjectDocuments { get; set; }

    public virtual DbSet<ProjectHRUCharacteristic> ProjectHRUCharacteristics { get; set; }

    public virtual DbSet<ProjectLoadGeneratingUnit> ProjectLoadGeneratingUnits { get; set; }

    public virtual DbSet<ProjectNereidResult> ProjectNereidResults { get; set; }

    public virtual DbSet<ProjectNetworkSolveHistory> ProjectNetworkSolveHistories { get; set; }

    public virtual DbSet<QuickBMP> QuickBMPs { get; set; }

    public virtual DbSet<RegionalSubbasin> RegionalSubbasins { get; set; }

    public virtual DbSet<RegionalSubbasinRevisionRequest> RegionalSubbasinRevisionRequests { get; set; }

    public virtual DbSet<RegionalSubbasinStaging> RegionalSubbasinStagings { get; set; }

    public virtual DbSet<SourceControlBMP> SourceControlBMPs { get; set; }

    public virtual DbSet<SourceControlBMPAttribute> SourceControlBMPAttributes { get; set; }

    public virtual DbSet<SpatialGridUnit> SpatialGridUnits { get; set; }

    public virtual DbSet<StateProvince> StateProvinces { get; set; }

    public virtual DbSet<StormwaterJurisdiction> StormwaterJurisdictions { get; set; }

    public virtual DbSet<StormwaterJurisdictionGeometry> StormwaterJurisdictionGeometries { get; set; }

    public virtual DbSet<StormwaterJurisdictionPerson> StormwaterJurisdictionPeople { get; set; }

    public virtual DbSet<SupportRequestLog> SupportRequestLogs { get; set; }

    public virtual DbSet<TrainingVideo> TrainingVideos { get; set; }

    public virtual DbSet<TrashGeneratingUnit> TrashGeneratingUnits { get; set; }

    public virtual DbSet<TrashGeneratingUnit4326> TrashGeneratingUnit4326s { get; set; }

    public virtual DbSet<TrashGeneratingUnitAdjustment> TrashGeneratingUnitAdjustments { get; set; }

    public virtual DbSet<TreatmentBMP> TreatmentBMPs { get; set; }

    public virtual DbSet<TreatmentBMPAssessment> TreatmentBMPAssessments { get; set; }

    public virtual DbSet<TreatmentBMPAssessmentObservationType> TreatmentBMPAssessmentObservationTypes { get; set; }

    public virtual DbSet<TreatmentBMPAssessmentPhoto> TreatmentBMPAssessmentPhotos { get; set; }

    public virtual DbSet<TreatmentBMPBenchmarkAndThreshold> TreatmentBMPBenchmarkAndThresholds { get; set; }

    public virtual DbSet<TreatmentBMPDocument> TreatmentBMPDocuments { get; set; }

    public virtual DbSet<TreatmentBMPImage> TreatmentBMPImages { get; set; }

    public virtual DbSet<TreatmentBMPModelingAttribute> TreatmentBMPModelingAttributes { get; set; }

    public virtual DbSet<TreatmentBMPObservation> TreatmentBMPObservations { get; set; }

    public virtual DbSet<TreatmentBMPType> TreatmentBMPTypes { get; set; }

    public virtual DbSet<TreatmentBMPTypeAssessmentObservationType> TreatmentBMPTypeAssessmentObservationTypes { get; set; }

    public virtual DbSet<TreatmentBMPTypeCustomAttributeType> TreatmentBMPTypeCustomAttributeTypes { get; set; }

    public virtual DbSet<WaterQualityManagementPlan> WaterQualityManagementPlans { get; set; }

    public virtual DbSet<WaterQualityManagementPlanBoundary> WaterQualityManagementPlanBoundaries { get; set; }

    public virtual DbSet<WaterQualityManagementPlanDocument> WaterQualityManagementPlanDocuments { get; set; }

    public virtual DbSet<WaterQualityManagementPlanExtractionResult> WaterQualityManagementPlanExtractionResults { get; set; }

    public virtual DbSet<WaterQualityManagementPlanParcel> WaterQualityManagementPlanParcels { get; set; }

    public virtual DbSet<WaterQualityManagementPlanPhoto> WaterQualityManagementPlanPhotos { get; set; }

    public virtual DbSet<WaterQualityManagementPlanVerify> WaterQualityManagementPlanVerifies { get; set; }

    public virtual DbSet<WaterQualityManagementPlanVerifyPhoto> WaterQualityManagementPlanVerifyPhotos { get; set; }

    public virtual DbSet<WaterQualityManagementPlanVerifyQuickBMP> WaterQualityManagementPlanVerifyQuickBMPs { get; set; }

    public virtual DbSet<WaterQualityManagementPlanVerifySourceControlBMP> WaterQualityManagementPlanVerifySourceControlBMPs { get; set; }

    public virtual DbSet<WaterQualityManagementPlanVerifyTreatmentBMP> WaterQualityManagementPlanVerifyTreatmentBMPs { get; set; }

    public virtual DbSet<Watershed> Watersheds { get; set; }

    public virtual DbSet<vFieldVisitDetailed> vFieldVisitDetaileds { get; set; }

    public virtual DbSet<vGeoServerLoadGeneratingUnit> vGeoServerLoadGeneratingUnits { get; set; }

    public virtual DbSet<vGeoServerWaterQualityManagementPlan> vGeoServerWaterQualityManagementPlans { get; set; }

    public virtual DbSet<vHRUCharacteristic> vHRUCharacteristics { get; set; }

    public virtual DbSet<vLoadGeneratingResult> vLoadGeneratingResults { get; set; }

    public virtual DbSet<vLoadGeneratingUnit> vLoadGeneratingUnits { get; set; }

    public virtual DbSet<vLoadGeneratingUnitUpdateCandidate> vLoadGeneratingUnitUpdateCandidates { get; set; }

    public virtual DbSet<vLoadReducingResult> vLoadReducingResults { get; set; }

    public virtual DbSet<vMaintenanceRecordDetailed> vMaintenanceRecordDetaileds { get; set; }

    public virtual DbSet<vModelingResultUnitConversion> vModelingResultUnitConversions { get; set; }

    public virtual DbSet<vMostRecentTreatmentBMPAssessment> vMostRecentTreatmentBMPAssessments { get; set; }

    public virtual DbSet<vNereidBMPColocation> vNereidBMPColocations { get; set; }

    public virtual DbSet<vNereidLoadingInput> vNereidLoadingInputs { get; set; }

    public virtual DbSet<vNereidProjectLoadingInput> vNereidProjectLoadingInputs { get; set; }

    public virtual DbSet<vNereidProjectRegionalSubbasinCentralizedBMP> vNereidProjectRegionalSubbasinCentralizedBMPs { get; set; }

    public virtual DbSet<vNereidProjectTreatmentBMPRegionalSubbasin> vNereidProjectTreatmentBMPRegionalSubbasins { get; set; }

    public virtual DbSet<vNereidRegionalSubbasinCentralizedBMP> vNereidRegionalSubbasinCentralizedBMPs { get; set; }

    public virtual DbSet<vNereidTreatmentBMPRegionalSubbasin> vNereidTreatmentBMPRegionalSubbasins { get; set; }

    public virtual DbSet<vOnlandVisualTrashAssessmentAreaProgress> vOnlandVisualTrashAssessmentAreaProgresses { get; set; }

    public virtual DbSet<vPowerBICentralizedBMPLoadGeneratingUnit> vPowerBICentralizedBMPLoadGeneratingUnits { get; set; }

    public virtual DbSet<vPowerBILandUseStatistic> vPowerBILandUseStatistics { get; set; }

    public virtual DbSet<vPowerBITreatmentBMP> vPowerBITreatmentBMPs { get; set; }

    public virtual DbSet<vPowerBIWaterQualityManagementPlan> vPowerBIWaterQualityManagementPlans { get; set; }

    public virtual DbSet<vPowerBIWaterQualityManagementPlanOAndMVerification> vPowerBIWaterQualityManagementPlanOAndMVerifications { get; set; }

    public virtual DbSet<vProjectDryWeatherWQLRIScore> vProjectDryWeatherWQLRIScores { get; set; }

    public virtual DbSet<vProjectGrantScore> vProjectGrantScores { get; set; }

    public virtual DbSet<vProjectLoadGeneratingResult> vProjectLoadGeneratingResults { get; set; }

    public virtual DbSet<vProjectLoadGeneratingUnitUpdateCandidate> vProjectLoadGeneratingUnitUpdateCandidates { get; set; }

    public virtual DbSet<vProjectLoadReducingResult> vProjectLoadReducingResults { get; set; }

    public virtual DbSet<vProjectWetWeatherWQLRIScore> vProjectWetWeatherWQLRIScores { get; set; }

    public virtual DbSet<vPyQgisDelineationLGUInput> vPyQgisDelineationLGUInputs { get; set; }

    public virtual DbSet<vPyQgisDelineationTGUInput> vPyQgisDelineationTGUInputs { get; set; }

    public virtual DbSet<vPyQgisLandUseBlockTGUInput> vPyQgisLandUseBlockTGUInputs { get; set; }

    public virtual DbSet<vPyQgisModelBasinLGUInput> vPyQgisModelBasinLGUInputs { get; set; }

    public virtual DbSet<vPyQgisOnlandVisualTrashAssessmentAreaDated> vPyQgisOnlandVisualTrashAssessmentAreaDateds { get; set; }

    public virtual DbSet<vPyQgisProjectDelineationLGUInput> vPyQgisProjectDelineationLGUInputs { get; set; }

    public virtual DbSet<vPyQgisRegionalSubbasinLGUInput> vPyQgisRegionalSubbasinLGUInputs { get; set; }

    public virtual DbSet<vPyQgisWaterQualityManagementPlanLGUInput> vPyQgisWaterQualityManagementPlanLGUInputs { get; set; }

    public virtual DbSet<vPyQgisWaterQualityManagementPlanTGUInput> vPyQgisWaterQualityManagementPlanTGUInputs { get; set; }

    public virtual DbSet<vRegionalSubbasinUpstream> vRegionalSubbasinUpstreams { get; set; }

    public virtual DbSet<vRegionalSubbasinUpstreamCatchmentGeometry> vRegionalSubbasinUpstreamCatchmentGeometries { get; set; }

    public virtual DbSet<vRegionalSubbasinUpstreamCatchmentGeometry4326> vRegionalSubbasinUpstreamCatchmentGeometry4326s { get; set; }

    public virtual DbSet<vTrashGeneratingUnitLoadStatistic> vTrashGeneratingUnitLoadStatistics { get; set; }

    public virtual DbSet<vTreatmentBMPAssessmentDetailed> vTreatmentBMPAssessmentDetaileds { get; set; }

    public virtual DbSet<vTreatmentBMPAssessmentObservationTypeDiscreteValue> vTreatmentBMPAssessmentObservationTypeDiscreteValues { get; set; }

    public virtual DbSet<vTreatmentBMPAssessmentObservationTypePassFail> vTreatmentBMPAssessmentObservationTypePassFails { get; set; }

    public virtual DbSet<vTreatmentBMPDetailed> vTreatmentBMPDetaileds { get; set; }

    public virtual DbSet<vTreatmentBMPGdbExport> vTreatmentBMPGdbExports { get; set; }

    public virtual DbSet<vTreatmentBMPModeledLandUseArea> vTreatmentBMPModeledLandUseAreas { get; set; }

    public virtual DbSet<vTreatmentBMPModelingAttribute> vTreatmentBMPModelingAttributes { get; set; }

    public virtual DbSet<vTreatmentBMPObservation> vTreatmentBMPObservations { get; set; }

    public virtual DbSet<vTreatmentBMPObservationDiscreteValue> vTreatmentBMPObservationDiscreteValues { get; set; }

    public virtual DbSet<vTreatmentBMPObservationPassFail> vTreatmentBMPObservationPassFails { get; set; }

    public virtual DbSet<vTreatmentBMPUpstream> vTreatmentBMPUpstreams { get; set; }

    public virtual DbSet<vWaterQualityManagementPlanAnnualReport> vWaterQualityManagementPlanAnnualReports { get; set; }

    public virtual DbSet<vWaterQualityManagementPlanDetailed> vWaterQualityManagementPlanDetaileds { get; set; }

    public virtual DbSet<vWaterQualityManagementPlanLGUAudit> vWaterQualityManagementPlanLGUAudits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AITokenUsage>(entity =>
        {
            entity.HasKey(e => e.AITokenUsageID).HasName("PK_AITokenUsage_AITokenUsageID");

            entity.HasOne(d => d.Person).WithMany(p => p.AITokenUsages).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<County>(entity =>
        {
            entity.HasKey(e => e.CountyID).HasName("PK_County_CountyID");

            entity.Property(e => e.CountyID).ValueGeneratedNever();

            entity.HasOne(d => d.StateProvince).WithMany(p => p.Counties).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CustomAttribute>(entity =>
        {
            entity.HasKey(e => e.CustomAttributeID).HasName("PK_CustomAttribute_CustomAttributeID");

            entity.HasOne(d => d.CustomAttributeType).WithMany(p => p.CustomAttributes).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMP).WithMany(p => p.CustomAttributes).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPTypeCustomAttributeType).WithMany(p => p.CustomAttributes).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPType).WithMany(p => p.CustomAttributes).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CustomAttributeType>(entity =>
        {
            entity.HasKey(e => e.CustomAttributeTypeID).HasName("PK_CustomAttributeType_CustomAttributeTypeID");
        });

        modelBuilder.Entity<CustomAttributeValue>(entity =>
        {
            entity.HasKey(e => e.CustomAttributeValueID).HasName("PK_CustomAttributeValue_CustomAttributeValueID");

            entity.HasOne(d => d.CustomAttribute).WithMany(p => p.CustomAttributeValues).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Delineation>(entity =>
        {
            entity.HasKey(e => e.DelineationID).HasName("PK_Delineation_DelineationID");

            entity.HasOne(d => d.TreatmentBMP).WithOne(p => p.Delineation).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.VerifiedByPerson).WithMany(p => p.Delineations).HasConstraintName("FK_Delineation_Person_VerifiedByPersonID_PersonID");
        });

        modelBuilder.Entity<DelineationOverlap>(entity =>
        {
            entity.HasKey(e => e.DelineationOverlapID).HasName("PK_DelineationOverlap_DelineationOverlapID");

            entity.Property(e => e.DelineationOverlapID).ValueGeneratedNever();

            entity.HasOne(d => d.Delineation).WithMany(p => p.DelineationOverlapDelineations).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.OverlappingDelineation).WithMany(p => p.DelineationOverlapOverlappingDelineations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DelineationOverlap_Delineation_OverlappingDelineationID_DelineationID");
        });

        modelBuilder.Entity<DelineationStaging>(entity =>
        {
            entity.HasKey(e => e.DelineationStagingID).HasName("PK_DelineationStaging_DelineationStagingID");

            entity.HasOne(d => d.StormwaterJurisdiction).WithMany(p => p.DelineationStagings).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.UploadedByPerson).WithMany(p => p.DelineationStagings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DelineationStaging_Person_UploadedByPersonID_PersonID");
        });

        modelBuilder.Entity<DirtyModelNode>(entity =>
        {
            entity.HasKey(e => e.DirtyModelNodeID).HasName("PK_DirtyModelNode_DirtyModelNodeID");
        });

        modelBuilder.Entity<FieldDefinition>(entity =>
        {
            entity.HasKey(e => e.FieldDefinitionID).HasName("PK_FieldDefinition_FieldDefinitionID");
        });

        modelBuilder.Entity<FieldVisit>(entity =>
        {
            entity.HasKey(e => e.FieldVisitID).HasName("PK_FieldVisit_FieldVisitID");

            entity.HasIndex(e => e.TreatmentBMPID, "CK_AtMostOneFieldVisitMayBeInProgressAtAnyTimePerBMP")
                .IsUnique()
                .HasFilter("([FieldVisitStatusID]=(1))");

            entity.HasOne(d => d.PerformedByPerson).WithMany(p => p.FieldVisits)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FieldVisit_Person_PerformedByPersonID_PersonID");

            entity.HasOne(d => d.TreatmentBMP).WithOne(p => p.FieldVisit).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<FileResource>(entity =>
        {
            entity.HasKey(e => e.FileResourceID).HasName("PK_FileResource_FileResourceID");

            entity.HasOne(d => d.CreatePerson).WithMany(p => p.FileResources)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FileResource_Person_CreatePersonID_PersonID");
        });

        modelBuilder.Entity<FundingEvent>(entity =>
        {
            entity.HasKey(e => e.FundingEventID).HasName("PK_FundingEvent_FundingEventID");

            entity.HasOne(d => d.TreatmentBMP).WithMany(p => p.FundingEvents).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<FundingEventFundingSource>(entity =>
        {
            entity.HasKey(e => e.FundingEventFundingSourceID).HasName("PK_FundingEventFundingSource_FundingEventFundingSourceID");

            entity.HasOne(d => d.FundingEvent).WithMany(p => p.FundingEventFundingSources).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.FundingSource).WithMany(p => p.FundingEventFundingSources).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<FundingSource>(entity =>
        {
            entity.HasKey(e => e.FundingSourceID).HasName("PK_FundingSource_FundingSourceID");

            entity.HasOne(d => d.Organization).WithMany(p => p.FundingSources).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<HRUCharacteristic>(entity =>
        {
            entity.HasKey(e => e.HRUCharacteristicID).HasName("PK_HRUCharacteristic_HRUCharacteristicID");

            entity.HasOne(d => d.LoadGeneratingUnit).WithMany(p => p.HRUCharacteristics).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<HRULog>(entity =>
        {
            entity.HasKey(e => e.HRULogID).HasName("PK_HRULog_HRULogID");
        });

        modelBuilder.Entity<HydrologicSubarea>(entity =>
        {
            entity.HasKey(e => e.HydrologicSubareaID).HasName("PK_HydrologicSubarea_HydrologicSubareaID");
        });

        modelBuilder.Entity<LandUseBlock>(entity =>
        {
            entity.HasKey(e => e.LandUseBlockID).HasName("PK_LandUseBlock_LandUseBlockID");

            entity.HasOne(d => d.StormwaterJurisdiction).WithMany(p => p.LandUseBlocks).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.UpdatePerson).WithMany(p => p.LandUseBlocks).HasConstraintName("FK_LandUseBlock_Person_UpdatePersonID_PersonID");
        });

        modelBuilder.Entity<LandUseBlockStaging>(entity =>
        {
            entity.HasKey(e => e.LandUseBlockStagingID).HasName("PK_LandUseBlockStaging_LandUseBlockStagingID");

            entity.HasOne(d => d.StormwaterJurisdiction).WithMany(p => p.LandUseBlockStagings).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.UploadedByPerson).WithMany(p => p.LandUseBlockStagings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LandUseBlockStaging_Person_UploadedByPersonID_PersonID");
        });

        modelBuilder.Entity<LoadGeneratingUnit>(entity =>
        {
            entity.HasKey(e => e.LoadGeneratingUnitID).HasName("PK_LoadGeneratingUnit_LoadGeneratingUnitID");
        });

        modelBuilder.Entity<LoadGeneratingUnitRefreshArea>(entity =>
        {
            entity.HasKey(e => e.LoadGeneratingUnitRefreshAreaID).HasName("PK_LoadGeneratingUnitRefreshArea_LoadGeneratingUnitRefreshAreaID");
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasKey(e => e.MaintenanceRecordID).HasName("PK_MaintenanceRecord_MaintenanceRecordID");

            entity.HasOne(d => d.FieldVisit).WithOne(p => p.MaintenanceRecord).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMP).WithMany(p => p.MaintenanceRecords).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPType).WithMany(p => p.MaintenanceRecords).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<MaintenanceRecordObservation>(entity =>
        {
            entity.HasKey(e => e.MaintenanceRecordObservationID).HasName("PK_MaintenanceRecordObservation_MaintenanceRecordObservationID");

            entity.HasOne(d => d.CustomAttributeType).WithMany(p => p.MaintenanceRecordObservations).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.MaintenanceRecord).WithMany(p => p.MaintenanceRecordObservations).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPTypeCustomAttributeType).WithMany(p => p.MaintenanceRecordObservations).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPType).WithMany(p => p.MaintenanceRecordObservations).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<MaintenanceRecordObservationValue>(entity =>
        {
            entity.HasKey(e => e.MaintenanceRecordObservationValueID).HasName("PK_MaintenanceRecordObservationValue_MaintenanceRecordObservationValueID");

            entity.HasOne(d => d.MaintenanceRecordObservation).WithMany(p => p.MaintenanceRecordObservationValues).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ModelBasin>(entity =>
        {
            entity.HasKey(e => e.ModelBasinID).HasName("PK_ModelBasin_ModelBasinID");
        });

        modelBuilder.Entity<ModelBasinStaging>(entity =>
        {
            entity.HasKey(e => e.ModelBasinStagingID).HasName("PK_ModelBasinStaging_ModelBasinStagingID");
        });

        modelBuilder.Entity<NeptuneHomePageImage>(entity =>
        {
            entity.HasKey(e => e.NeptuneHomePageImageID).HasName("PK_NeptuneHomePageImage_NeptuneHomePageImageID");

            entity.HasOne(d => d.FileResource).WithMany(p => p.NeptuneHomePageImages).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<NeptunePage>(entity =>
        {
            entity.HasKey(e => e.NeptunePageID).HasName("PK_NeptunePage_NeptunePageID");
        });

        modelBuilder.Entity<NeptunePageImage>(entity =>
        {
            entity.HasKey(e => e.NeptunePageImageID).HasName("PK_NeptunePageImage_NeptunePageImageID");

            entity.HasOne(d => d.FileResource).WithMany(p => p.NeptunePageImages).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.NeptunePage).WithMany(p => p.NeptunePageImages).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<NereidLog>(entity =>
        {
            entity.HasKey(e => e.NereidLogID).HasName("PK_NereidLog_NereidLogID");
        });

        modelBuilder.Entity<NereidResult>(entity =>
        {
            entity.HasKey(e => e.NereidResultID).HasName("PK_NereidResult_NereidResultID");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationID).HasName("PK_Notification_NotificationID");

            entity.HasOne(d => d.Person).WithMany(p => p.Notifications).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<OCTAPrioritization>(entity =>
        {
            entity.HasKey(e => e.OCTAPrioritizationID).HasName("PK_OCTAPrioritization_OCTAPrioritizationID");
        });

        modelBuilder.Entity<OCTAPrioritizationStaging>(entity =>
        {
            entity.HasKey(e => e.OCTAPrioritizationStagingID).HasName("PK_OCTAPrioritizationStaging_OCTAPrioritizationStagingID");
        });

        modelBuilder.Entity<OnlandVisualTrashAssessment>(entity =>
        {
            entity.HasKey(e => e.OnlandVisualTrashAssessmentID).HasName("PK_OnlandVisualTrashAssessment_OnlandVisualTrashAssessmentID");

            entity.HasOne(d => d.CreatedByPerson).WithMany(p => p.OnlandVisualTrashAssessments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlandVisualTrashAssessment_Person_CreatedByPersonID_PersonID");

            entity.HasOne(d => d.StormwaterJurisdiction).WithMany(p => p.OnlandVisualTrashAssessments).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<OnlandVisualTrashAssessmentArea>(entity =>
        {
            entity.HasKey(e => e.OnlandVisualTrashAssessmentAreaID).HasName("PK_OnlandVisualTrashAssessmentArea_OnlandVisualTrashAssessmentAreaID");

            entity.HasOne(d => d.StormwaterJurisdiction).WithMany(p => p.OnlandVisualTrashAssessmentAreas).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<OnlandVisualTrashAssessmentAreaStaging>(entity =>
        {
            entity.HasKey(e => e.OnlandVisualTrashAssessmentAreaStagingID).HasName("PK_OnlandVisualTrashAssessmentAreaStaging_OnlandVisualTrashAssessmentAreaStagingID");

            entity.HasOne(d => d.StormwaterJurisdiction).WithMany(p => p.OnlandVisualTrashAssessmentAreaStagings).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.UploadedByPerson).WithMany(p => p.OnlandVisualTrashAssessmentAreaStagings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlandVisualTrashAssessmentAreaStaging_Person_UploadedByPersonID_PersonID");
        });

        modelBuilder.Entity<OnlandVisualTrashAssessmentObservation>(entity =>
        {
            entity.HasKey(e => e.OnlandVisualTrashAssessmentObservationID).HasName("PK_OnlandVisualTrashAssessmentObservation_OnlandVisualTrashAssessmentObservationID");

            entity.HasOne(d => d.OnlandVisualTrashAssessment).WithMany(p => p.OnlandVisualTrashAssessmentObservations).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<OnlandVisualTrashAssessmentObservationPhoto>(entity =>
        {
            entity.HasKey(e => e.OnlandVisualTrashAssessmentObservationPhotoID).HasName("PK_OnlandVisualTrashAssessmentObservationPhoto_OnlandVisualTrashAssessmentObservationPhotoID");

            entity.HasOne(d => d.FileResource).WithMany(p => p.OnlandVisualTrashAssessmentObservationPhotos).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.OnlandVisualTrashAssessmentObservation).WithMany(p => p.OnlandVisualTrashAssessmentObservationPhotos).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<OnlandVisualTrashAssessmentObservationPhotoStaging>(entity =>
        {
            entity.HasKey(e => e.OnlandVisualTrashAssessmentObservationPhotoStagingID).HasName("PK_OnlandVisualTrashAssessmentObservationPhotoStaging_OnlandVisualTrashAssessmentObservationPhotoStagingID");

            entity.HasOne(d => d.FileResource).WithMany(p => p.OnlandVisualTrashAssessmentObservationPhotoStagings).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.OnlandVisualTrashAssessment).WithMany(p => p.OnlandVisualTrashAssessmentObservationPhotoStagings).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<OnlandVisualTrashAssessmentPreliminarySourceIdentificationType>(entity =>
        {
            entity.HasKey(e => e.OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypeID).HasName("PK_OnlandVisualTrashAssessmentPreliminarySourceIdentificationType_OnlandVisualTrashAssessmentPreliminarySourceIdentificationType");

            entity.HasOne(d => d.OnlandVisualTrashAssessment).WithMany(p => p.OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.OrganizationID).HasName("PK_Organization_OrganizationID");

            entity.HasOne(d => d.LogoFileResource).WithMany(p => p.Organizations).HasConstraintName("FK_Organization_FileResource_LogoFileResourceID_FileResourceID");

            entity.HasOne(d => d.OrganizationType).WithMany(p => p.Organizations).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.PrimaryContactPerson).WithMany(p => p.Organizations).HasConstraintName("FK_Organization_Person_PrimaryContactPersonID_PersonID");
        });

        modelBuilder.Entity<OrganizationType>(entity =>
        {
            entity.HasKey(e => e.OrganizationTypeID).HasName("PK_OrganizationType_OrganizationTypeID");
        });

        modelBuilder.Entity<Parcel>(entity =>
        {
            entity.HasKey(e => e.ParcelID).HasName("PK_Parcel_ParcelID");

            entity.Property(e => e.LastUpdate).HasDefaultValue(new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        });

        modelBuilder.Entity<ParcelGeometry>(entity =>
        {
            entity.HasKey(e => e.ParcelGeometryID).HasName("PK_ParcelGeometry_ParcelGeometryID");

            entity.HasOne(d => d.Parcel).WithOne(p => p.ParcelGeometry).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ParcelStaging>(entity =>
        {
            entity.HasKey(e => e.ParcelStagingID).HasName("PK_ParcelStaging_ParcelStagingID");
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.PersonID).HasName("PK_Person_PersonID");

            entity.HasOne(d => d.Organization).WithMany(p => p.People).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PrecipitationZone>(entity =>
        {
            entity.HasKey(e => e.PrecipitationZoneID).HasName("PK_PrecipitationZone_PrecipitationZoneID");
        });

        modelBuilder.Entity<PrecipitationZoneStaging>(entity =>
        {
            entity.HasKey(e => e.PrecipitationZoneStagingID).HasName("PK_PrecipitationZoneStaging_PrecipitationZoneStagingID");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectID).HasName("PK_Project_ProjectID");

            entity.HasOne(d => d.CreatePerson).WithMany(p => p.ProjectCreatePeople)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Project_Person_CreatePersonID_PersonID");

            entity.HasOne(d => d.Organization).WithMany(p => p.Projects).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.PrimaryContactPerson).WithMany(p => p.ProjectPrimaryContactPeople)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Project_Person_PrimaryContactPersonID_PersonID");

            entity.HasOne(d => d.StormwaterJurisdiction).WithMany(p => p.Projects).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.UpdatePerson).WithMany(p => p.ProjectUpdatePeople).HasConstraintName("FK_Project_Person_UpdatePersonID_PersonID");
        });

        modelBuilder.Entity<ProjectDocument>(entity =>
        {
            entity.HasKey(e => e.ProjectDocumentID).HasName("PK_ProjectDocument_ProjectDocumentID");

            entity.HasOne(d => d.FileResource).WithMany(p => p.ProjectDocuments).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectDocuments).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProjectHRUCharacteristic>(entity =>
        {
            entity.HasKey(e => e.ProjectHRUCharacteristicID).HasName("PK_ProjectHRUCharacteristic_ProjectHRUCharacteristicID");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectHRUCharacteristics).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ProjectLoadGeneratingUnit).WithMany(p => p.ProjectHRUCharacteristics).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProjectLoadGeneratingUnit>(entity =>
        {
            entity.HasKey(e => e.ProjectLoadGeneratingUnitID).HasName("PK_ProjectLoadGeneratingUnit_ProjectLoadGeneratingUnitID");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectLoadGeneratingUnits).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProjectNereidResult>(entity =>
        {
            entity.HasKey(e => e.ProjectNereidResultID).HasName("PK_ProjectNereidResult_ProjectNereidResultID");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectNereidResults).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProjectNetworkSolveHistory>(entity =>
        {
            entity.HasKey(e => e.ProjectNetworkSolveHistoryID).HasName("PK_ProjectNetworkSolveHistory_ProjectNetworkSolveHistoryID");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectNetworkSolveHistories).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.RequestedByPerson).WithMany(p => p.ProjectNetworkSolveHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectNetworkSolveHistory_Person_RequestedByPersonID_PersonID");
        });

        modelBuilder.Entity<QuickBMP>(entity =>
        {
            entity.HasKey(e => e.QuickBMPID).HasName("PK_QuickBMP_QuickBMPID");

            entity.Property(e => e.NumberOfIndividualBMPs).HasDefaultValue(1);

            entity.HasOne(d => d.TreatmentBMPType).WithMany(p => p.QuickBMPs).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WaterQualityManagementPlan).WithMany(p => p.QuickBMPs).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<RegionalSubbasin>(entity =>
        {
            entity.HasKey(e => e.RegionalSubbasinID).HasName("PK_RegionalSubbasin_RegionalSubbasinID");

            entity.HasOne(d => d.OCSurveyDownstreamCatchment).WithMany(p => p.InverseOCSurveyDownstreamCatchment)
                .HasPrincipalKey(p => p.OCSurveyCatchmentID)
                .HasForeignKey(d => d.OCSurveyDownstreamCatchmentID)
                .HasConstraintName("FK_RegionalSubbasin_RegionalSubbasin_OCSurveyDownstreamCatchmentID_OCSurveyCatchmentID");
        });

        modelBuilder.Entity<RegionalSubbasinRevisionRequest>(entity =>
        {
            entity.HasKey(e => e.RegionalSubbasinRevisionRequestID).HasName("PK_RegionalSubbasinRevisionRequest_RegionalSubbasinRevisionRequestID");

            entity.HasOne(d => d.ClosedByPerson).WithMany(p => p.RegionalSubbasinRevisionRequestClosedByPeople).HasConstraintName("FK_RegionalSubbasinRevisionRequest_Person_ClosedByPersonID_PersonID");

            entity.HasOne(d => d.RequestPerson).WithMany(p => p.RegionalSubbasinRevisionRequestRequestPeople)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RegionalSubbasinRevisionRequest_Person_RequestPersonID_PersonID");

            entity.HasOne(d => d.TreatmentBMP).WithMany(p => p.RegionalSubbasinRevisionRequests).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<RegionalSubbasinStaging>(entity =>
        {
            entity.HasKey(e => e.RegionalSubbasinStagingID).HasName("PK_RegionalSubbasinStaging_RegionalSubbasinStagingID");
        });

        modelBuilder.Entity<SourceControlBMP>(entity =>
        {
            entity.HasKey(e => e.SourceControlBMPID).HasName("PK_SourceControlBMP_SourceControlBMPID");

            entity.HasOne(d => d.SourceControlBMPAttribute).WithMany(p => p.SourceControlBMPs).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WaterQualityManagementPlan).WithMany(p => p.SourceControlBMPs).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SourceControlBMPAttribute>(entity =>
        {
            entity.HasKey(e => e.SourceControlBMPAttributeID).HasName("PK_SourceControlBMPAttribute_SourceControlBMPAttributeID");

            entity.Property(e => e.SourceControlBMPAttributeID).ValueGeneratedNever();
        });

        modelBuilder.Entity<SpatialGridUnit>(entity =>
        {
            entity.HasKey(e => e.SpatialGridUnitID).HasName("PK_SpatialGridUnit_SpatialGridUnitID");

            entity.Property(e => e.SpatialGridUnitID).ValueGeneratedNever();
        });

        modelBuilder.Entity<StateProvince>(entity =>
        {
            entity.HasKey(e => e.StateProvinceID).HasName("PK_StateProvince_StateProvinceID");

            entity.Property(e => e.StateProvinceID).ValueGeneratedNever();
            entity.Property(e => e.StateProvinceAbbreviation).IsFixedLength();
        });

        modelBuilder.Entity<StormwaterJurisdiction>(entity =>
        {
            entity.HasKey(e => e.StormwaterJurisdictionID).HasName("PK_StormwaterJurisdiction_StormwaterJurisdictionID");

            entity.HasOne(d => d.Organization).WithOne(p => p.StormwaterJurisdiction).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.StateProvince).WithMany(p => p.StormwaterJurisdictions).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<StormwaterJurisdictionGeometry>(entity =>
        {
            entity.HasKey(e => e.StormwaterJurisdictionGeometryID).HasName("PK_StormwaterJurisdictionGeometry_StormwaterJurisdictionGeometryID");

            entity.HasOne(d => d.StormwaterJurisdiction).WithOne(p => p.StormwaterJurisdictionGeometry).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<StormwaterJurisdictionPerson>(entity =>
        {
            entity.HasKey(e => e.StormwaterJurisdictionPersonID).HasName("PK_StormwaterJurisdictionPerson_StormwaterJurisdictionPersonID");

            entity.HasOne(d => d.Person).WithMany(p => p.StormwaterJurisdictionPeople).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.StormwaterJurisdiction).WithMany(p => p.StormwaterJurisdictionPeople).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SupportRequestLog>(entity =>
        {
            entity.HasKey(e => e.SupportRequestLogID).HasName("PK_SupportRequestLog_SupportRequestLogID");

            entity.HasOne(d => d.RequestPerson).WithMany(p => p.SupportRequestLogs).HasConstraintName("FK_SupportRequestLog_Person_RequestPersonID_PersonID");
        });

        modelBuilder.Entity<TrainingVideo>(entity =>
        {
            entity.HasKey(e => e.TrainingVideoID).HasName("PK_TrainingVideo_TrainingVideoID");
        });

        modelBuilder.Entity<TrashGeneratingUnit>(entity =>
        {
            entity.HasKey(e => e.TrashGeneratingUnitID).HasName("PK_TrashGeneratingUnit_TrashGeneratingUnitID");

            entity.Property(e => e.LastUpdateDate).HasDefaultValueSql("(getdate())", "DF_TrashGeneratingUnit_LastUpdateDate");

            entity.HasOne(d => d.StormwaterJurisdiction).WithMany(p => p.TrashGeneratingUnits).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TrashGeneratingUnit4326>(entity =>
        {
            entity.HasKey(e => e.TrashGeneratingUnit4326ID).HasName("PK_TrashGeneratingUnit4326_TrashGeneratingUnit4326ID");

            entity.Property(e => e.LastUpdateDate).HasDefaultValueSql("(getdate())", "DF_TrashGeneratingUnit4326_LastUpdateDate");

            entity.HasOne(d => d.StormwaterJurisdiction).WithMany(p => p.TrashGeneratingUnit4326s).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TrashGeneratingUnitAdjustment>(entity =>
        {
            entity.HasKey(e => e.TrashGeneratingUnitAdjustmentID).HasName("PK_TrashGeneratingUnitAdjustment_TrashGeneratingUnitAdjustmentID");

            entity.HasOne(d => d.AdjustedByPerson).WithMany(p => p.TrashGeneratingUnitAdjustments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrashGeneratingUnitAdjustment_Person_AdjustedByPersonID_PersonID");
        });

        modelBuilder.Entity<TreatmentBMP>(entity =>
        {
            entity.HasKey(e => e.TreatmentBMPID).HasName("PK_TreatmentBMP_TreatmentBMPID");

            entity.HasIndex(e => new { e.StormwaterJurisdictionID, e.TreatmentBMPName }, "AK_TreatmentBMP_StormwaterJurisdictionID_TreatmentBMPName")
                .IsUnique()
                .HasFilter("([ProjectID] IS NULL)");

            entity.HasOne(d => d.InventoryVerifiedByPerson).WithMany(p => p.TreatmentBMPs).HasConstraintName("FK_TreatmentBMP_Person_InventoryVerifiedByPersonID_PersonID");

            entity.HasOne(d => d.LastNereidLog).WithMany(p => p.TreatmentBMPs).HasConstraintName("FK_TreatmentBMP_NereidLog_LastNereidLogID_NereidLogID");

            entity.HasOne(d => d.OwnerOrganization).WithMany(p => p.TreatmentBMPs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TreatmentBMP_Organization_OwnerOrganizationID_OrganizationID");

            entity.HasOne(d => d.StormwaterJurisdiction).WithMany(p => p.TreatmentBMPs).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPType).WithMany(p => p.TreatmentBMPs).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.UpstreamBMP).WithMany(p => p.InverseUpstreamBMP).HasConstraintName("FK_TreatmentBMP_TreatmentBMP_UpstreamBMPID_TreatmentBMPID");
        });

        modelBuilder.Entity<TreatmentBMPAssessment>(entity =>
        {
            entity.HasKey(e => e.TreatmentBMPAssessmentID).HasName("PK_TreatmentBMPAssessment_TreatmentBMPAssessmentID");

            entity.HasOne(d => d.FieldVisit).WithMany(p => p.TreatmentBMPAssessments).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMP).WithMany(p => p.TreatmentBMPAssessments).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPType).WithMany(p => p.TreatmentBMPAssessments).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TreatmentBMPAssessmentObservationType>(entity =>
        {
            entity.HasKey(e => e.TreatmentBMPAssessmentObservationTypeID).HasName("PK_TreatmentBMPAssessmentObservationType_TreatmentBMPAssessmentObservationTypeID");
        });

        modelBuilder.Entity<TreatmentBMPAssessmentPhoto>(entity =>
        {
            entity.HasKey(e => e.TreatmentBMPAssessmentPhotoID).HasName("PK_TreatmentBMPAssessmentPhoto_TreatmentBMPAssessmentPhotoID");

            entity.HasOne(d => d.FileResource).WithMany(p => p.TreatmentBMPAssessmentPhotos).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPAssessment).WithMany(p => p.TreatmentBMPAssessmentPhotos).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TreatmentBMPBenchmarkAndThreshold>(entity =>
        {
            entity.HasKey(e => e.TreatmentBMPBenchmarkAndThresholdID).HasName("PK_TreatmentBMPBenchmarkAndThreshold_TreatmentBMPBenchmarkAndThresholdID");

            entity.HasOne(d => d.TreatmentBMPAssessmentObservationType).WithMany(p => p.TreatmentBMPBenchmarkAndThresholds).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMP).WithMany(p => p.TreatmentBMPBenchmarkAndThresholds).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPTypeAssessmentObservationType).WithMany(p => p.TreatmentBMPBenchmarkAndThresholds).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPType).WithMany(p => p.TreatmentBMPBenchmarkAndThresholds).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TreatmentBMPDocument>(entity =>
        {
            entity.HasKey(e => e.TreatmentBMPDocumentID).HasName("PK_TreatmentBMPDocument_TreatmentBMPDocumentID");

            entity.HasOne(d => d.FileResource).WithMany(p => p.TreatmentBMPDocuments).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMP).WithMany(p => p.TreatmentBMPDocuments).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TreatmentBMPImage>(entity =>
        {
            entity.HasKey(e => e.TreatmentBMPImageID).HasName("PK_TreatmentBMPImage_TreatmentBMPImageID");

            entity.HasOne(d => d.FileResource).WithMany(p => p.TreatmentBMPImages).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMP).WithMany(p => p.TreatmentBMPImages).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TreatmentBMPModelingAttribute>(entity =>
        {
            entity.HasKey(e => e.TreatmentBMPModelingAttributeID).HasName("PK_TreatmentBMPModelingAttribute_TreatmentBMPModelingAttributeID");

            entity.HasOne(d => d.TreatmentBMP).WithOne(p => p.TreatmentBMPModelingAttributeTreatmentBMP).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.UpstreamTreatmentBMP).WithMany(p => p.TreatmentBMPModelingAttributeUpstreamTreatmentBMPs).HasConstraintName("FK_TreatmentBMPModelingAttribute_TreatmentBMP_UpstreamTreatmentBMPID_TreatmentBMPID");
        });

        modelBuilder.Entity<TreatmentBMPObservation>(entity =>
        {
            entity.HasKey(e => e.TreatmentBMPObservationID).HasName("PK_TreatmentBMPObservation_TreatmentBMPObservationID");

            entity.HasOne(d => d.TreatmentBMPAssessment).WithMany(p => p.TreatmentBMPObservations).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPAssessmentObservationType).WithMany(p => p.TreatmentBMPObservations).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPTypeAssessmentObservationType).WithMany(p => p.TreatmentBMPObservations).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPType).WithMany(p => p.TreatmentBMPObservations).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TreatmentBMPType>(entity =>
        {
            entity.HasKey(e => e.TreatmentBMPTypeID).HasName("PK_TreatmentBMPType_TreatmentBMPTypeID");
        });

        modelBuilder.Entity<TreatmentBMPTypeAssessmentObservationType>(entity =>
        {
            entity.HasKey(e => e.TreatmentBMPTypeAssessmentObservationTypeID).HasName("PK_TreatmentBMPTypeAssessmentObservationType_TreatmentBMPTypeAssessmentObservationTypeID");

            entity.HasOne(d => d.TreatmentBMPAssessmentObservationType).WithMany(p => p.TreatmentBMPTypeAssessmentObservationTypes).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPType).WithMany(p => p.TreatmentBMPTypeAssessmentObservationTypes).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TreatmentBMPTypeCustomAttributeType>(entity =>
        {
            entity.HasKey(e => e.TreatmentBMPTypeCustomAttributeTypeID).HasName("PK_TreatmentBMPTypeCustomAttributeType_TreatmentBMPTypeCustomAttributeTypeID");

            entity.HasOne(d => d.CustomAttributeType).WithMany(p => p.TreatmentBMPTypeCustomAttributeTypes).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TreatmentBMPType).WithMany(p => p.TreatmentBMPTypeCustomAttributeTypes).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WaterQualityManagementPlan>(entity =>
        {
            entity.HasKey(e => e.WaterQualityManagementPlanID).HasName("PK_WaterQualityManagementPlan_WaterQualityManagementPlanID");

            entity.HasOne(d => d.LastNereidLog).WithMany(p => p.WaterQualityManagementPlans).HasConstraintName("FK_WaterQualityManagementPlan_NereidLog_LastNereidLogID_NereidLogID");

            entity.HasOne(d => d.StormwaterJurisdiction).WithMany(p => p.WaterQualityManagementPlans).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WaterQualityManagementPlanBoundary>(entity =>
        {
            entity.HasKey(e => e.WaterQualityManagementPlanGeometryID).HasName("PK_WaterQualityManagementPlanGeometry_WaterQualityManagementPlanGeometryID");

            entity.HasOne(d => d.WaterQualityManagementPlan).WithOne(p => p.WaterQualityManagementPlanBoundary)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WaterQualityManagementPlanGeometry_WaterQualityManagementPlan_WaterQualityManagementPlanID");
        });

        modelBuilder.Entity<WaterQualityManagementPlanDocument>(entity =>
        {
            entity.HasKey(e => e.WaterQualityManagementPlanDocumentID).HasName("PK_WaterQualityManagementPlanDocument_WaterQualityManagementPlanDocumentID");

            entity.HasOne(d => d.FileResource).WithMany(p => p.WaterQualityManagementPlanDocuments).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WaterQualityManagementPlan).WithMany(p => p.WaterQualityManagementPlanDocuments).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WaterQualityManagementPlanExtractionResult>(entity =>
        {
            entity.HasKey(e => e.WaterQualityManagementPlanExtractionResultID).HasName("PK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlanExtractionResultID");

            entity.HasOne(d => d.WaterQualityManagementPlanDocument).WithMany(p => p.WaterQualityManagementPlanExtractionResults).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WaterQualityManagementPlan).WithOne(p => p.WaterQualityManagementPlanExtractionResult).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WaterQualityManagementPlanParcel>(entity =>
        {
            entity.HasKey(e => e.WaterQualityManagementPlanParcelID).HasName("PK_WaterQualityManagementPlanParcel_WaterQualityManagementPlanParcelID");

            entity.HasOne(d => d.Parcel).WithMany(p => p.WaterQualityManagementPlanParcels).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WaterQualityManagementPlan).WithMany(p => p.WaterQualityManagementPlanParcels).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WaterQualityManagementPlanPhoto>(entity =>
        {
            entity.HasKey(e => e.WaterQualityManagementPlanPhotoID).HasName("PK_WaterQualityManagementPlanPhoto_WaterQualityManagementPlanPhotoID");

            entity.HasOne(d => d.FileResource).WithMany(p => p.WaterQualityManagementPlanPhotos).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WaterQualityManagementPlanVerify>(entity =>
        {
            entity.HasKey(e => e.WaterQualityManagementPlanVerifyID).HasName("PK_WaterQualityManagementPlanVerify_WaterQualityManagementPlanVerifyID");

            entity.HasOne(d => d.LastEditedByPerson).WithMany(p => p.WaterQualityManagementPlanVerifies)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WaterQualityManagementPlanVerify_Person_LastEditedByPersonID_PersonID");

            entity.HasOne(d => d.WaterQualityManagementPlan).WithMany(p => p.WaterQualityManagementPlanVerifies).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WaterQualityManagementPlanVerifyPhoto>(entity =>
        {
            entity.HasKey(e => e.WaterQualityManagementPlanVerifyPhotoID).HasName("PK_WaterQualityManagementPlanVerifyPhoto_WaterQualityManagementPlanVerifyPhotoID");

            entity.HasOne(d => d.WaterQualityManagementPlanPhoto).WithMany(p => p.WaterQualityManagementPlanVerifyPhotos).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WaterQualityManagementPlanVerify).WithMany(p => p.WaterQualityManagementPlanVerifyPhotos).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WaterQualityManagementPlanVerifyQuickBMP>(entity =>
        {
            entity.HasKey(e => e.WaterQualityManagementPlanVerifyQuickBMPID).HasName("PK_WaterQualityManagementPlanVerifyQuickBMP_WaterQualityManagementPlanVerifyQuickBMPID");

            entity.HasOne(d => d.QuickBMP).WithMany(p => p.WaterQualityManagementPlanVerifyQuickBMPs).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WaterQualityManagementPlanVerify).WithMany(p => p.WaterQualityManagementPlanVerifyQuickBMPs).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WaterQualityManagementPlanVerifySourceControlBMP>(entity =>
        {
            entity.HasKey(e => e.WaterQualityManagementPlanVerifySourceControlBMPID).HasName("PK_WaterQualityManagementPlanVerifySourceControlBMP_WaterQualityManagementPlanVerifySourceControlBMPID");

            entity.HasOne(d => d.SourceControlBMP).WithMany(p => p.WaterQualityManagementPlanVerifySourceControlBMPs).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WaterQualityManagementPlanVerify).WithMany(p => p.WaterQualityManagementPlanVerifySourceControlBMPs).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WaterQualityManagementPlanVerifyTreatmentBMP>(entity =>
        {
            entity.HasKey(e => e.WaterQualityManagementPlanVerifyTreatmentBMPID).HasName("PK_WaterQualityManagementPlanVerifyTreatmentBMP_WaterQualityManagementPlanVerifyTreatmentBMPID");

            entity.HasOne(d => d.TreatmentBMP).WithMany(p => p.WaterQualityManagementPlanVerifyTreatmentBMPs).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WaterQualityManagementPlanVerify).WithMany(p => p.WaterQualityManagementPlanVerifyTreatmentBMPs).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Watershed>(entity =>
        {
            entity.HasKey(e => e.WatershedID).HasName("PK_Watershed_WatershedID");
        });

        modelBuilder.Entity<vFieldVisitDetailed>(entity =>
        {
            entity.ToView("vFieldVisitDetailed");
        });

        modelBuilder.Entity<vGeoServerLoadGeneratingUnit>(entity =>
        {
            entity.ToView("vGeoServerLoadGeneratingUnit");

            entity.Property(e => e.LoadGeneratingUnitID).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<vGeoServerWaterQualityManagementPlan>(entity =>
        {
            entity.ToView("vGeoServerWaterQualityManagementPlan");
        });

        modelBuilder.Entity<vHRUCharacteristic>(entity =>
        {
            entity.ToView("vHRUCharacteristic");
        });

        modelBuilder.Entity<vLoadGeneratingResult>(entity =>
        {
            entity.ToView("vLoadGeneratingResult");
        });

        modelBuilder.Entity<vLoadGeneratingUnit>(entity =>
        {
            entity.ToView("vLoadGeneratingUnit");
        });

        modelBuilder.Entity<vLoadGeneratingUnitUpdateCandidate>(entity =>
        {
            entity.ToView("vLoadGeneratingUnitUpdateCandidate");
        });

        modelBuilder.Entity<vLoadReducingResult>(entity =>
        {
            entity.ToView("vLoadReducingResult");
        });

        modelBuilder.Entity<vMaintenanceRecordDetailed>(entity =>
        {
            entity.ToView("vMaintenanceRecordDetailed");
        });

        modelBuilder.Entity<vModelingResultUnitConversion>(entity =>
        {
            entity.ToView("vModelingResultUnitConversion");
        });

        modelBuilder.Entity<vMostRecentTreatmentBMPAssessment>(entity =>
        {
            entity.ToView("vMostRecentTreatmentBMPAssessment");
        });

        modelBuilder.Entity<vNereidBMPColocation>(entity =>
        {
            entity.ToView("vNereidBMPColocation");
        });

        modelBuilder.Entity<vNereidLoadingInput>(entity =>
        {
            entity.ToView("vNereidLoadingInput");
        });

        modelBuilder.Entity<vNereidProjectLoadingInput>(entity =>
        {
            entity.ToView("vNereidProjectLoadingInput");
        });

        modelBuilder.Entity<vNereidProjectRegionalSubbasinCentralizedBMP>(entity =>
        {
            entity.ToView("vNereidProjectRegionalSubbasinCentralizedBMP");
        });

        modelBuilder.Entity<vNereidProjectTreatmentBMPRegionalSubbasin>(entity =>
        {
            entity.ToView("vNereidProjectTreatmentBMPRegionalSubbasin");
        });

        modelBuilder.Entity<vNereidRegionalSubbasinCentralizedBMP>(entity =>
        {
            entity.ToView("vNereidRegionalSubbasinCentralizedBMP");
        });

        modelBuilder.Entity<vNereidTreatmentBMPRegionalSubbasin>(entity =>
        {
            entity.ToView("vNereidTreatmentBMPRegionalSubbasin");
        });

        modelBuilder.Entity<vOnlandVisualTrashAssessmentAreaProgress>(entity =>
        {
            entity.ToView("vOnlandVisualTrashAssessmentAreaProgress");
        });

        modelBuilder.Entity<vPowerBICentralizedBMPLoadGeneratingUnit>(entity =>
        {
            entity.ToView("vPowerBICentralizedBMPLoadGeneratingUnit");
        });

        modelBuilder.Entity<vPowerBILandUseStatistic>(entity =>
        {
            entity.ToView("vPowerBILandUseStatistic");
        });

        modelBuilder.Entity<vPowerBITreatmentBMP>(entity =>
        {
            entity.ToView("vPowerBITreatmentBMP");
        });

        modelBuilder.Entity<vPowerBIWaterQualityManagementPlan>(entity =>
        {
            entity.ToView("vPowerBIWaterQualityManagementPlan");
        });

        modelBuilder.Entity<vPowerBIWaterQualityManagementPlanOAndMVerification>(entity =>
        {
            entity.ToView("vPowerBIWaterQualityManagementPlanOAndMVerification");
        });

        modelBuilder.Entity<vProjectDryWeatherWQLRIScore>(entity =>
        {
            entity.ToView("vProjectDryWeatherWQLRIScore");
        });

        modelBuilder.Entity<vProjectGrantScore>(entity =>
        {
            entity.ToView("vProjectGrantScore");
        });

        modelBuilder.Entity<vProjectLoadGeneratingResult>(entity =>
        {
            entity.ToView("vProjectLoadGeneratingResult");
        });

        modelBuilder.Entity<vProjectLoadGeneratingUnitUpdateCandidate>(entity =>
        {
            entity.ToView("vProjectLoadGeneratingUnitUpdateCandidate");
        });

        modelBuilder.Entity<vProjectLoadReducingResult>(entity =>
        {
            entity.ToView("vProjectLoadReducingResult");
        });

        modelBuilder.Entity<vProjectWetWeatherWQLRIScore>(entity =>
        {
            entity.ToView("vProjectWetWeatherWQLRIScore");
        });

        modelBuilder.Entity<vPyQgisDelineationLGUInput>(entity =>
        {
            entity.ToView("vPyQgisDelineationLGUInput");
        });

        modelBuilder.Entity<vPyQgisDelineationTGUInput>(entity =>
        {
            entity.ToView("vPyQgisDelineationTGUInput");
        });

        modelBuilder.Entity<vPyQgisLandUseBlockTGUInput>(entity =>
        {
            entity.ToView("vPyQgisLandUseBlockTGUInput");

            entity.Property(e => e.LUBID).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<vPyQgisModelBasinLGUInput>(entity =>
        {
            entity.ToView("vPyQgisModelBasinLGUInput");

            entity.Property(e => e.ModelID).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<vPyQgisOnlandVisualTrashAssessmentAreaDated>(entity =>
        {
            entity.ToView("vPyQgisOnlandVisualTrashAssessmentAreaDated");
        });

        modelBuilder.Entity<vPyQgisProjectDelineationLGUInput>(entity =>
        {
            entity.ToView("vPyQgisProjectDelineationLGUInput");
        });

        modelBuilder.Entity<vPyQgisRegionalSubbasinLGUInput>(entity =>
        {
            entity.ToView("vPyQgisRegionalSubbasinLGUInput");

            entity.Property(e => e.RSBID).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<vPyQgisWaterQualityManagementPlanLGUInput>(entity =>
        {
            entity.ToView("vPyQgisWaterQualityManagementPlanLGUInput");
        });

        modelBuilder.Entity<vPyQgisWaterQualityManagementPlanTGUInput>(entity =>
        {
            entity.ToView("vPyQgisWaterQualityManagementPlanTGUInput");
        });

        modelBuilder.Entity<vRegionalSubbasinUpstream>(entity =>
        {
            entity.ToView("vRegionalSubbasinUpstream");
        });

        modelBuilder.Entity<vRegionalSubbasinUpstreamCatchmentGeometry>(entity =>
        {
            entity.ToView("vRegionalSubbasinUpstreamCatchmentGeometry");
        });

        modelBuilder.Entity<vRegionalSubbasinUpstreamCatchmentGeometry4326>(entity =>
        {
            entity.ToView("vRegionalSubbasinUpstreamCatchmentGeometry4326");
        });

        modelBuilder.Entity<vTrashGeneratingUnitLoadStatistic>(entity =>
        {
            entity.ToView("vTrashGeneratingUnitLoadStatistic");
        });

        modelBuilder.Entity<vTreatmentBMPAssessmentDetailed>(entity =>
        {
            entity.ToView("vTreatmentBMPAssessmentDetailed");
        });

        modelBuilder.Entity<vTreatmentBMPAssessmentObservationTypeDiscreteValue>(entity =>
        {
            entity.ToView("vTreatmentBMPAssessmentObservationTypeDiscreteValue");
        });

        modelBuilder.Entity<vTreatmentBMPAssessmentObservationTypePassFail>(entity =>
        {
            entity.ToView("vTreatmentBMPAssessmentObservationTypePassFail");
        });

        modelBuilder.Entity<vTreatmentBMPDetailed>(entity =>
        {
            entity.ToView("vTreatmentBMPDetailed");
        });

        modelBuilder.Entity<vTreatmentBMPGdbExport>(entity =>
        {
            entity.ToView("vTreatmentBMPGdbExport");
        });

        modelBuilder.Entity<vTreatmentBMPModeledLandUseArea>(entity =>
        {
            entity.ToView("vTreatmentBMPModeledLandUseArea");
        });

        modelBuilder.Entity<vTreatmentBMPModelingAttribute>(entity =>
        {
            entity.ToView("vTreatmentBMPModelingAttribute");
        });

        modelBuilder.Entity<vTreatmentBMPObservation>(entity =>
        {
            entity.ToView("vTreatmentBMPObservation");

            entity.Property(e => e.TreatmentBMPObservationID).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<vTreatmentBMPObservationDiscreteValue>(entity =>
        {
            entity.ToView("vTreatmentBMPObservationDiscreteValue");
        });

        modelBuilder.Entity<vTreatmentBMPObservationPassFail>(entity =>
        {
            entity.ToView("vTreatmentBMPObservationPassFail");
        });

        modelBuilder.Entity<vTreatmentBMPUpstream>(entity =>
        {
            entity.ToView("vTreatmentBMPUpstream");
        });

        modelBuilder.Entity<vWaterQualityManagementPlanAnnualReport>(entity =>
        {
            entity.ToView("vWaterQualityManagementPlanAnnualReport");
        });

        modelBuilder.Entity<vWaterQualityManagementPlanDetailed>(entity =>
        {
            entity.ToView("vWaterQualityManagementPlanDetailed");
        });

        modelBuilder.Entity<vWaterQualityManagementPlanLGUAudit>(entity =>
        {
            entity.ToView("vWaterQualityManagementPlanLGUAudit");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
