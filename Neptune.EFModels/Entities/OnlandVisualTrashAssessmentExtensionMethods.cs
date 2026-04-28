using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class OnlandVisualTrashAssessmentExtensionMethods
{
    public static OnlandVisualTrashAssessmentSimpleDto AsSimpleDto(this OnlandVisualTrashAssessment onlandVisualTrashAssessment)
    {
        var dto = new OnlandVisualTrashAssessmentSimpleDto()
        {
            OnlandVisualTrashAssessmentID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID,
            CreatedByPersonID = onlandVisualTrashAssessment.CreatedByPersonID,
            CreatedDate = onlandVisualTrashAssessment.CreatedDate,
            OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID,
            Notes = onlandVisualTrashAssessment.Notes,
            StormwaterJurisdictionID = onlandVisualTrashAssessment.StormwaterJurisdictionID,
            AssessingNewArea = onlandVisualTrashAssessment.AssessingNewArea,
            OnlandVisualTrashAssessmentStatusID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentStatusID,
            IsDraftGeometryManuallyRefined = onlandVisualTrashAssessment.IsDraftGeometryManuallyRefined,
            OnlandVisualTrashAssessmentScoreID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentScoreID,
            CompletedDate = onlandVisualTrashAssessment.CompletedDate,
            DraftAreaName = onlandVisualTrashAssessment.DraftAreaName,
            DraftAreaDescription = onlandVisualTrashAssessment.DraftAreaDescription,
            IsTransectBackingAssessment = onlandVisualTrashAssessment.IsTransectBackingAssessment,
            IsProgressAssessment = onlandVisualTrashAssessment.IsProgressAssessment
        };
        return dto;
    }

    public static OnlandVisualTrashAssessmentGridDto AsGridDto(this OnlandVisualTrashAssessment onlandVisualTrashAssessment)
    {
        var dto = new OnlandVisualTrashAssessmentGridDto()
        {
            OnlandVisualTrashAssessmentID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID,
            CreatedByPersonFullName = onlandVisualTrashAssessment.CreatedByPerson.GetFullNameFirstLast(),
            CreatedDate = onlandVisualTrashAssessment.CreatedDate,
            OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID,
            OnlandVisualTrashAssessmentAreaName = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea?.OnlandVisualTrashAssessmentAreaName,
            Notes = onlandVisualTrashAssessment.Notes,
            StormwaterJurisdictionID = onlandVisualTrashAssessment.StormwaterJurisdictionID,
            StormwaterJurisdictionName = onlandVisualTrashAssessment.StormwaterJurisdiction?.GetOrganizationDisplayName(),
            OnlandVisualTrashAssessmentStatusID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentStatusID,
            OnlandVisualTrashAssessmentStatusName = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentStatus.OnlandVisualTrashAssessmentStatusDisplayName,
            OnlandVisualTrashAssessmentScoreName = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentScore?.OnlandVisualTrashAssessmentScoreDisplayName,
            CompletedDate = onlandVisualTrashAssessment.CompletedDate,
            IsProgressAssessment = onlandVisualTrashAssessment.IsProgressAssessment ? "Progress" : "Baseline"
        };
        return dto;
    }

    public static OnlandVisualTrashAssessmentDetailDto AsDetailDto(this OnlandVisualTrashAssessment onlandVisualTrashAssessment)
    {
        var dto = new OnlandVisualTrashAssessmentDetailDto()
        {
            OnlandVisualTrashAssessmentID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID,
            CreatedByPersonFullName = onlandVisualTrashAssessment.CreatedByPerson.GetFullNameFirstLast(),
            CreatedDate = onlandVisualTrashAssessment.CreatedDate,
            OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID,
            OnlandVisualTrashAssessmentAreaName = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea?.OnlandVisualTrashAssessmentAreaName,
            Notes = onlandVisualTrashAssessment.Notes,
            StormwaterJurisdictionID = onlandVisualTrashAssessment.StormwaterJurisdictionID,
            StormwaterJurisdictionName = onlandVisualTrashAssessment.StormwaterJurisdiction.GetOrganizationDisplayName(),
            OnlandVisualTrashAssessmentStatusID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentStatusID,
            OnlandVisualTrashAssessmentStatusName = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentStatus.OnlandVisualTrashAssessmentStatusDisplayName,
            OnlandVisualTrashAssessmentScoreName = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentScore?.OnlandVisualTrashAssessmentScoreDisplayName,
            CompletedDate = onlandVisualTrashAssessment.CompletedDate,
            IsProgressAssessment = onlandVisualTrashAssessment.IsProgressAssessment ? "Progress" : "Baseline",
        };
        dto.PreliminarySourceIdentificationsByCategory = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes.GroupBy(x => x.PreliminarySourceIdentificationType.PreliminarySourceIdentificationCategoryID).ToDictionary(x => x.Key.ToString(), x => x.Select(y => 
            !string.IsNullOrWhiteSpace(y.ExplanationIfTypeIsOther) ? $"Other: {y.ExplanationIfTypeIsOther}"
                :
            y.PreliminarySourceIdentificationType.PreliminarySourceIdentificationTypeDisplayName).ToList());
        return dto;
    }

    public static OnlandVisualTrashAssessmentAddRemoveParcelsDto AsAddRemoveParcelDto(
        this OnlandVisualTrashAssessment onlandVisualTrashAssessment, NeptuneDbContext dbContext)
    {
        var dto = new OnlandVisualTrashAssessmentAddRemoveParcelsDto()
        {
            OnlandVisualTrashAssessmentID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID,
            OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID,
            StormwaterJurisdictionID = onlandVisualTrashAssessment.StormwaterJurisdictionID,
            IsDraftGeometryManuallyRefined = onlandVisualTrashAssessment.IsDraftGeometryManuallyRefined ?? false,
            SelectedParcelIDs = onlandVisualTrashAssessment.GetParcelIDsForAddOrRemoveParcels(dbContext)
        };
        return dto;
    }

    public static OnlandVisualTrashAssessmentReviewAndFinalizeDto AsReviewAndFinalizeDto(this OnlandVisualTrashAssessment onlandVisualTrashAssessment)
    {
        var dto = new OnlandVisualTrashAssessmentReviewAndFinalizeDto()
        {
            OnlandVisualTrashAssessmentID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID,
            OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID,
            OnlandVisualTrashAssessmentAreaName = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea != null ? onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentAreaName : onlandVisualTrashAssessment.DraftAreaName,
            Notes = onlandVisualTrashAssessment.Notes,
            StormwaterJurisdictionID = onlandVisualTrashAssessment.StormwaterJurisdictionID,
            OnlandVisualTrashAssessmentScoreID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentScoreID,
            AssessmentAreaDescription = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea != null ? onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea.AssessmentAreaDescription : onlandVisualTrashAssessment.DraftAreaDescription,
            AssessingNewArea = onlandVisualTrashAssessment.AssessingNewArea ?? false,
            IsProgressAssessment = onlandVisualTrashAssessment.IsProgressAssessment,
            // Preserve the originally-finalized Assessment Date on return-to-edit. Fall back to
            // today only when CompletedDate has never been set (initial finalize of a new OVTA),
            // so the form still pre-fills a reasonable default for brand-new records.
            AssessmentDate = onlandVisualTrashAssessment.CompletedDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            OnlandVisualTrashAssessmentStatusID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentStatusID,
        };

        var selectedPreliminarySourceIdentifications = onlandVisualTrashAssessment
            .OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes
            .Select(x => new OnlandVisualTrashAssessmentPreliminarySourceIdentificationUpsertDto
            {
                Selected = true,
                PreliminarySourceIdentificationTypeID = x.PreliminarySourceIdentificationTypeID,
                PreliminarySourceIdentificationTypeName = x.PreliminarySourceIdentificationType.PreliminarySourceIdentificationTypeDisplayName,
                PreliminarySourceIdentificationCategoryID = x.PreliminarySourceIdentificationType.PreliminarySourceIdentificationCategoryID,
                IsOther = x.PreliminarySourceIdentificationType.IsOther(),
                ExplanationIfTypeIsOther = x.ExplanationIfTypeIsOther
            }).ToList();

        var notSelectedPreliminarySourceIdentifications = PreliminarySourceIdentificationType.All.Where(x => !onlandVisualTrashAssessment.OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes.Select(y => y.PreliminarySourceIdentificationTypeID).Contains(x.PreliminarySourceIdentificationTypeID)).Select(x => new OnlandVisualTrashAssessmentPreliminarySourceIdentificationUpsertDto
        {
            Selected = false,
            PreliminarySourceIdentificationTypeID = x.PreliminarySourceIdentificationTypeID,
            PreliminarySourceIdentificationTypeName = x.PreliminarySourceIdentificationTypeDisplayName,
            PreliminarySourceIdentificationCategoryID = x.PreliminarySourceIdentificationCategoryID,
            IsOther = x.IsOther(),

        });
        selectedPreliminarySourceIdentifications.AddRange(notSelectedPreliminarySourceIdentifications);

        dto.PreliminarySourceIdentifications = selectedPreliminarySourceIdentifications.OrderBy(x => x.IsOther).ThenBy(x => x.PreliminarySourceIdentificationTypeName).ToList();
        return dto;
    }

}