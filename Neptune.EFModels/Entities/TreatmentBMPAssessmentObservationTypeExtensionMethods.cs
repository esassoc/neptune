using Neptune.Models.DataTransferObjects;
using Neptune.Common;

namespace Neptune.EFModels.Entities;

public static partial class TreatmentBMPAssessmentObservationTypeExtensionMethods
{

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