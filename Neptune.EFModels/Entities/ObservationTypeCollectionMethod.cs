using System.ComponentModel.DataAnnotations;
using Neptune.Common.GeoSpatial;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities
{
    public partial class ObservationTypeCollectionMethod
    {
        public abstract bool ValidateObservationTypeJson(string json);
        public abstract List<ValidationResult> ValidateObservationType(string json);
        public abstract List<ValidationResult> ValidateObservationDataJson(
            TreatmentBMPAssessmentObservationType treatmentBMPAssessmentObservationType, string json);

        public abstract double? GetObservationValueFromObservationData(string observationData);

        public abstract double? CalculateScore(TreatmentBMPObservation treatmentBMPObservation, TreatmentBMP treatmentBMP);

        public virtual string CalculateOverrideScoreText(string assessmentScoreIfFailing, string observationTypeSchema, bool overrideAssessmentScoreIfFailing)
        {
            return string.Empty;
        }
    }

    public partial class ObservationTypeCollectionMethodDiscreteValue
    {
        public override bool ValidateObservationTypeJson(string json)
        {
            try
            {
                var schema = GeoJsonSerializer.Deserialize<DiscreteObservationTypeSchema>(json);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public override List<ValidationResult> ValidateObservationType(string json)
        {
            var validationErrors = new List<ValidationResult>();
            var schema = GeoJsonSerializer.Deserialize<DiscreteObservationTypeSchema>(json);

            var propertiesToObserve = schema.PropertiesToObserve;
            TreatmentBMPAssessmentObservationTypeHelper.ValidatePropertiesToObserve(propertiesToObserve, validationErrors);
            TreatmentBMPAssessmentObservationTypeHelper.ValidateNumberOfObservations(schema.MinimumNumberOfObservations, schema.MaximumNumberOfObservations, validationErrors);
            TreatmentBMPAssessmentObservationTypeHelper.ValidateValueOfObservations(schema.MinimumValueOfObservations, schema.MaximumValueOfObservations, validationErrors);
            TreatmentBMPAssessmentObservationTypeHelper.ValidateMeasurementUnitLabel(schema.MeasurementUnitLabel, validationErrors);
            TreatmentBMPAssessmentObservationTypeHelper.ValidateMeasurementUnitTypeID(schema.MeasurementUnitTypeID, validationErrors);
            TreatmentBMPAssessmentObservationTypeHelper.ValidateAssessmentInstructions(schema.AssessmentDescription, validationErrors);
            TreatmentBMPAssessmentObservationTypeHelper.ValidateBenchmarkAndThresholdDescription(schema.BenchmarkDescription, schema.ThresholdDescription, validationErrors);

            return validationErrors;
        }

        public override List<ValidationResult> ValidateObservationDataJson(
            TreatmentBMPAssessmentObservationType treatmentBMPAssessmentObservationType, string json)
        {
            var validationResults = new List<ValidationResult>();
            try
            {
                var schema = GeoJsonSerializer.Deserialize<DiscreteObservationSchema>(json);
                var treatmentBMPAssessmentObservationTypeName = treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName;
                if (!(schema.SingleValueObservations.Count > 0))
                {
                    validationResults.Add(new ValidationResult($"You must enter at least one observation for '{treatmentBMPAssessmentObservationTypeName}'."));
                }

                if (schema.SingleValueObservations.Any(x => x.ObservationValue == null))
                {
                    validationResults.Add(new ValidationResult($"Values for the observation '{treatmentBMPAssessmentObservationTypeName}' cannot be blank."));
                }
            }
            catch (Exception)
            {
                validationResults.Add(new ValidationResult("Schema invalid"));
            }

            return validationResults;
        }

        public override double? GetObservationValueFromObservationData(string observationData)
        {
            var observation = GeoJsonSerializer.Deserialize<DiscreteObservationSchema>(observationData);
            // A blank ObservationValue is a valid persisted state (validation flags it but doesn't block
            // save), so skip null/unparseable values rather than NRE on .ToString().
            var values = observation.SingleValueObservations
                .Where(x => double.TryParse(x.ObservationValue?.ToString(), out _))
                .Select(x => double.Parse(x.ObservationValue.ToString()))
                .ToList();
            return values.Count > 0 ? values.Average() : null;
        }

        public override double? CalculateScore(TreatmentBMPObservation treatmentBMPObservation, TreatmentBMP treatmentBMP)
        {            
            var observationValue = GetObservationValueFromObservationData(treatmentBMPObservation.ObservationData);
            var treatmentBMPBenchmarkAndThresholds = treatmentBMP.TreatmentBMPBenchmarkAndThresholds;
            var treatmentBMPAssessmentObservationType = treatmentBMPObservation.TreatmentBMPAssessmentObservationType;
            var benchmarkValue = treatmentBMPAssessmentObservationType.GetBenchmarkValue(treatmentBMPBenchmarkAndThresholds);
            var thresholdValue = treatmentBMPAssessmentObservationType.GetThresholdValue(treatmentBMPBenchmarkAndThresholds);

            var useUpperValue = treatmentBMPAssessmentObservationType.UseUpperValueForThreshold(benchmarkValue, observationValue);
            var thresholdValueInBenchmarkUnits = treatmentBMPAssessmentObservationType.GetThresholdValueInBenchmarkUnits(benchmarkValue, thresholdValue, useUpperValue);

            if (observationValue == null || benchmarkValue == null || thresholdValueInBenchmarkUnits == null)
            {
                return null;
            }

            return TreatmentBMPAssessmentObservationTypeHelper.LinearInterpolation(observationValue.Value, benchmarkValue.Value, thresholdValueInBenchmarkUnits.Value);
        }
    }

    public partial class ObservationTypeCollectionMethodPassFail
    {
        public override bool ValidateObservationTypeJson(string json)
        {
            try
            {
                var schema = GeoJsonSerializer.Deserialize<PassFailObservationTypeSchema>(json);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public override List<ValidationResult> ValidateObservationType(string json)
        {
            var validationErrors = new List<ValidationResult>();
            var schema = GeoJsonSerializer.Deserialize<PassFailObservationTypeSchema>(json);

            var propertiesToObserve = schema.PropertiesToObserve;
            TreatmentBMPAssessmentObservationTypeHelper.ValidatePropertiesToObserve(propertiesToObserve, validationErrors);
            TreatmentBMPAssessmentObservationTypeHelper.ValidateAssessmentInstructions(schema.AssessmentDescription, validationErrors);
            TreatmentBMPAssessmentObservationTypeHelper.ValidateRequiredStringField(schema.PassingScoreLabel, "Passing Score Label must have a name and cannot be blank", validationErrors);
            TreatmentBMPAssessmentObservationTypeHelper.ValidateRequiredStringField(schema.FailingScoreLabel, "Failing Score Label must have a name and cannot be blank", validationErrors);

            return validationErrors;
        }

        public override List<ValidationResult> ValidateObservationDataJson(
            TreatmentBMPAssessmentObservationType treatmentBMPAssessmentObservationType, string json)
        {
            var validationResults = new List<ValidationResult>();
            try
            {
                var schema = GeoJsonSerializer.Deserialize<PassFailObservationSchema>(json);
                var treatmentBMPAssessmentObservationTypeName = treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName;
                if (schema.SingleValueObservations.Any(x => x.ObservationValue == null))
                {
                    validationResults.Add(new ValidationResult($"Values for the observation '{treatmentBMPAssessmentObservationTypeName}' cannot be blank."));
                }
            }
            catch (Exception)
            {
                validationResults.Add(new ValidationResult("Schema invalid"));
            }

            return validationResults;
        }

        public override double? GetObservationValueFromObservationData(string observationData)
        {
            var observation = GeoJsonSerializer.Deserialize<PassFailObservationSchema>(observationData);
            // Distinguish "no valid value" (return null) from "all values passed": skip blank/unparseable
            // values, and if none remain don't report a passing score that would mask incomplete data.
            var passFailValues = observation.SingleValueObservations
                .Where(x => bool.TryParse(x.ObservationValue?.ToString(), out _))
                .Select(x => bool.Parse(x.ObservationValue.ToString()))
                .ToList();
            if (passFailValues.Count == 0)
            {
                return null;
            }
            var conveyanceFails = passFailValues.Any(passed => !passed);
            return conveyanceFails ? 0 : 5;
        }

        public override double? CalculateScore(TreatmentBMPObservation treatmentBMPObservation, TreatmentBMP treatmentBMP)
        {
            var observationValue = GetObservationValueFromObservationData(treatmentBMPObservation.ObservationData);
            return observationValue;
        }

        public override string CalculateOverrideScoreText(string observationData,
            string observationTypeSchema,
            bool overrideAssessmentScoreIfFailing)
        {
            var observation = GeoJsonSerializer.Deserialize<PassFailObservationSchema>(observationData);
            var passFailValues = observation.SingleValueObservations
                .Where(x => bool.TryParse(x.ObservationValue?.ToString(), out _))
                .Select(x => bool.Parse(x.ObservationValue.ToString()))
                .ToList();
            if (passFailValues.Count == 0)
            {
                // No valid value yet — don't label a blank observation as passing.
                return string.Empty;
            }
            var schema = GeoJsonSerializer.Deserialize<PassFailObservationTypeSchema>(observationTypeSchema);
            var conveyanceFails = passFailValues.Any(passed => !passed);
            return conveyanceFails ? schema.FailingScoreLabel : schema.PassingScoreLabel;
        }
    }

    public partial class ObservationTypeCollectionMethodPercentage
    {
        public override bool ValidateObservationTypeJson(string json)
        {
            try
            {
                var schema = GeoJsonSerializer.Deserialize<PercentageObservationTypeSchema>(json);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public override List<ValidationResult> ValidateObservationType(string json)
        {
            var validationErrors = new List<ValidationResult>();
            var schema = GeoJsonSerializer.Deserialize<PercentageObservationTypeSchema>(json);

            var propertiesToObserve = schema.PropertiesToObserve;
            TreatmentBMPAssessmentObservationTypeHelper.ValidatePropertiesToObserve(propertiesToObserve, validationErrors);
            TreatmentBMPAssessmentObservationTypeHelper.ValidateMeasurementUnitLabel(schema.MeasurementUnitLabel, validationErrors);
            TreatmentBMPAssessmentObservationTypeHelper.ValidateAssessmentInstructions(schema.AssessmentDescription, validationErrors);
            TreatmentBMPAssessmentObservationTypeHelper.ValidateBenchmarkAndThresholdDescription(schema.BenchmarkDescription, schema.ThresholdDescription, validationErrors);

            return validationErrors;
        }
        public override List<ValidationResult> ValidateObservationDataJson(
            TreatmentBMPAssessmentObservationType treatmentBMPAssessmentObservationType, string json)
        {
            var validationResults = new List<ValidationResult>();
            try
            {
                var schema = GeoJsonSerializer.Deserialize<DiscreteObservationSchema>(json);
                var treatmentBMPAssessmentObservationTypeName = treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName;
                if (!(schema.SingleValueObservations.Count > 0))
                {
                    validationResults.Add(new ValidationResult($"You must enter at least one observation for '{treatmentBMPAssessmentObservationTypeName}'."));
                }

                if (schema.SingleValueObservations.Any(x => x.ObservationValue == null))
                {
                    validationResults.Add(new ValidationResult($"Values for the observation '{treatmentBMPAssessmentObservationTypeName}' cannot be blank."));
                }
            }
            catch (Exception)
            {
                validationResults.Add(new ValidationResult("Schema invalid"));
            }

            return validationResults;
        }

        public override double? GetObservationValueFromObservationData(string observationData)
        {
            var observation = GeoJsonSerializer.Deserialize<PercentageObservationSchema>(observationData);
            // A blank ObservationValue is a valid persisted state (validation flags it but doesn't block
            // save), so skip null/unparseable values rather than NRE on .ToString().
            var values = observation.SingleValueObservations
                .Where(x => double.TryParse(x.ObservationValue?.ToString(), out _))
                .Select(x => double.Parse(x.ObservationValue.ToString()))
                .ToList();
            return values.Count > 0 ? values.Sum() : null;
        }

        public override double? CalculateScore(TreatmentBMPObservation treatmentBMPObservation, TreatmentBMP treatmentBMP)
        {
            var observationValue = GetObservationValueFromObservationData(treatmentBMPObservation.ObservationData);
            var treatmentBMPBenchmarkAndThresholds = treatmentBMPObservation.TreatmentBMPAssessment.TreatmentBMP.TreatmentBMPBenchmarkAndThresholds;
            var benchmarkValue = treatmentBMPObservation.TreatmentBMPAssessmentObservationType.GetBenchmarkValue(treatmentBMPBenchmarkAndThresholds);
            var thresholdValue = treatmentBMPObservation.TreatmentBMPAssessmentObservationType.GetThresholdValue(treatmentBMPBenchmarkAndThresholds);
            var useUpperValue = treatmentBMPObservation.TreatmentBMPAssessmentObservationType.UseUpperValueForThreshold(benchmarkValue, observationValue);

            var thresholdValueInBenchmarkUnits = treatmentBMPObservation.TreatmentBMPAssessmentObservationType.GetThresholdValueInBenchmarkUnits(benchmarkValue, thresholdValue, useUpperValue);

            if (observationValue == null || benchmarkValue == null || thresholdValueInBenchmarkUnits == null)
            {
                return null;
            }

            return TreatmentBMPAssessmentObservationTypeHelper.LinearInterpolation(observationValue.Value, benchmarkValue.Value, thresholdValueInBenchmarkUnits.Value);
        }
    }
}