using Neptune.Models.DataTransferObjects;
using Neptune.Common;

namespace Neptune.EFModels.Entities;

public static partial class TreatmentBMPAssessmentObservationTypeExtensionMethods
{
    public static TreatmentBMPAssessmentObservationTypeDetailedDto AsDetailedDto(this TreatmentBMPAssessmentObservationType treatmentBMPAssessmentObservationType)
    {
        var treatmentBMPAssessmentObservationTypeDetailedDto = new TreatmentBMPAssessmentObservationTypeDetailedDto()
        {
            TreatmentBMPAssessmentObservationTypeID = treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeID,
            HasBenchmarkAndThresholds = treatmentBMPAssessmentObservationType.GetHasBenchmarkAndThreshold(),
            TargetIsSweetSpot = treatmentBMPAssessmentObservationType.GetTargetIsSweetSpot(),
            TreatmentBMPAssessmentObservationTypeName = $"{treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName}",
            BenchmarkUnitLegendDisplayName = treatmentBMPAssessmentObservationType.BenchmarkMeasurementUnitType()?.LegendDisplayName ?? string.Empty,
            ThresholdUnitLegendDisplayName = treatmentBMPAssessmentObservationType.ThresholdMeasurementUnitType()?.LegendDisplayName ?? string.Empty,
            CollectionMethodDisplayName = treatmentBMPAssessmentObservationType.ObservationTypeSpecification.ObservationTypeCollectionMethod.ObservationTypeCollectionMethodDisplayName
        };
        return treatmentBMPAssessmentObservationTypeDetailedDto;
    }

    public static TreatmentBMPAssessmentObservationTypeForScoringDto AsForScoringDto(
        this TreatmentBMPAssessmentObservationType treatmentBMPAssessmentObservationType, TreatmentBMPAssessment treatmentBMPAssessment, bool overrideAssessmentScoreIfFailing)
    {
        var treatmentBMPBenchmarkAndThresholds = treatmentBMPAssessment.TreatmentBMP.TreatmentBMPBenchmarkAndThresholds;
        var benchmarkValue = treatmentBMPAssessmentObservationType.GetBenchmarkValue(treatmentBMPBenchmarkAndThresholds);
        var thresholdValue = treatmentBMPAssessmentObservationType.GetThresholdValue(treatmentBMPBenchmarkAndThresholds);
        var assessmentScoreWeight = treatmentBMPAssessmentObservationType.TreatmentBMPTypeAssessmentObservationTypes.SingleOrDefault(x => x.TreatmentBMPTypeID == treatmentBMPAssessment.TreatmentBMPTypeID)?.AssessmentScoreWeight;
        var treatmentBMPObservation = treatmentBMPAssessment.TreatmentBMPObservations.SingleOrDefault(y => y.TreatmentBMPAssessmentObservationTypeID == treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeID);
        var observationScoreDto = treatmentBMPObservation?.AsObservationScoreDto(overrideAssessmentScoreIfFailing);
        var useUpperValue = treatmentBMPAssessmentObservationType.UseUpperValueForThreshold(benchmarkValue, observationScoreDto?.ObservationValue);

        var treatmentBMPAssessmentObservationTypeForScoringDto = new TreatmentBMPAssessmentObservationTypeForScoringDto()
        {
            TreatmentBMPAssessmentObservationTypeID = treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeID,
            HasBenchmarkAndThresholds = treatmentBMPAssessmentObservationType.GetHasBenchmarkAndThreshold(),
            DisplayName = $"{treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName}{(treatmentBMPAssessmentObservationType.GetMeasurementUnitType() != null ? $" ({treatmentBMPAssessmentObservationType.GetMeasurementUnitType()?.LegendDisplayName})" : string.Empty)}",
            TreatmentBMPObservationSimple = observationScoreDto,
            ThresholdValueInObservedUnits = treatmentBMPAssessmentObservationType.GetThresholdValueInBenchmarkUnits(benchmarkValue, thresholdValue, useUpperValue) ?? 0,
            BenchmarkValue = benchmarkValue ?? 0,
            Weight = assessmentScoreWeight?.ToStringShortPercent() ?? "pass/fail",
        };
        return treatmentBMPAssessmentObservationTypeForScoringDto;
    }
}