/*-----------------------------------------------------------------------
<copyright file="ObservationTypeHelper.cs" company="Tahoe Regional Planning Agency">
Copyright (c) Tahoe Regional Planning Agency. All rights reserved.
<author>Sitka Technology Group</author>
</copyright>

<license>
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License <http://www.gnu.org/licenses/> for more details.

Source code is available upon request via <support@sitkatech.com>.
</license>
-----------------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Neptune.EFModels.Entities
{
    public static class TreatmentBMPAssessmentObservationTypeHelper
    {
        public static double LinearInterpolation(double observation, double benchmark, double threshold)
        {
            var score = benchmark > threshold ? PositiveLinearInterpolation(observation, benchmark, threshold) : NegativeLinearInterpolation(observation, benchmark, threshold);
            return score < 0 ? 0 : (score > 5 ? 5 : score);
        }

        public static double PositiveLinearInterpolation(double observation, double benchmark, double threshold)
        {
            // y = y0 + (x - x0) * ((y1 - y0)/(x1-x0)) where y0 = 2 and y1 = 5
            var score = 2 + (observation - threshold)*((5 - 2)/(benchmark - threshold));
            return Math.Round(score, 1);
        }

        public static double NegativeLinearInterpolation(double observation, double benchmark, double threshold)
        {
            // y = y0 + (x - x0) * ((y1 - y0)/(x1-x0)) where y0 = 5 and y1 = 2
            var score = 5 + (observation - benchmark)*((2 - 5)/(threshold - benchmark));
            return Math.Round(score,1);
        }

        public static void ValidateAssessmentInstructions(string assessmentInstructions, List<ValidationResult> validationErrors)
        {
            ValidateRequiredStringField(assessmentInstructions, "Assessment Instructions cannot be blank", validationErrors);
        }

        public static void ValidateBenchmarkAndThresholdDescription(string benchmarkDescription, string thresholdDescription, List<ValidationResult> validationErrors)
        {
            ValidateRequiredStringField(benchmarkDescription, "Benchmark Instructions cannot be blank", validationErrors);
            ValidateRequiredStringField(thresholdDescription, "Threshold Instructions cannot be blank", validationErrors);
        }

        public static void ValidateMeasurementUnitLabel(string measurementUnitLabel, List<ValidationResult> validationErrors)
        {
            ValidateRequiredStringField(measurementUnitLabel, "Measurement Unit Label must have a name and cannot be blank", validationErrors);
        }

        public static void ValidatePropertiesToObserve(List<string> propertiesToObserve, List<ValidationResult> validationErrors)
        {
            if (propertiesToObserve.Distinct().Count() < propertiesToObserve.Count)
            {
                validationErrors.Add(new ValidationResult("Properties to Observe must have unique names"));
            }

            if (propertiesToObserve.Any(string.IsNullOrWhiteSpace))
            {
                validationErrors.Add(new ValidationResult("Each Property to Observe must have a name and cannot be blank"));
            }

            if (propertiesToObserve.Count.Equals(0))
            {
                validationErrors.Add(new ValidationResult("At least one Property to Observe is required"));
            }
        }

        public static void ValidateNumberOfObservations(int minimumNumberOfObservations, int? maximumNumberOfObservations, List<ValidationResult> validationErrors)
        {            
            if (minimumNumberOfObservations == 0)
            {
                validationErrors.Add(new ValidationResult("Minimum Number of Observations must be greater than 0"));
            }

            if (maximumNumberOfObservations != null && minimumNumberOfObservations > maximumNumberOfObservations)
            {
                validationErrors.Add(new ValidationResult("Minimum Number of Observations must be less than or equal to the Maximum Number of Observations"));
            }
        }

        public static void ValidateValueOfObservations(double minimumValueOfObservations, double? maximumValueOfObservations, List<ValidationResult> validationErrors)
        {           
            if (maximumValueOfObservations != null && minimumValueOfObservations > maximumValueOfObservations)
            {
                validationErrors.Add(new ValidationResult("Minimum Value of Each Observation must be less than or equal to the Maximum Value of Each Observation"));
            }
        }

        public static void ValidateMeasurementUnitTypeID(int measurementUnitTypeID, List<ValidationResult> validationErrors)
        {
            if (MeasurementUnitType.All.SingleOrDefault(x => x.MeasurementUnitTypeID == measurementUnitTypeID) == null)
            {
                validationErrors.Add(new ValidationResult("A valid Measurement Unit Type is required"));
            }
        }

        public static void ValidateRequiredStringField(string propertyToValidate, string passingScoreLabelMustHaveANameAndCannotBeBlank, List<ValidationResult> validationErrors)
        {
            if (string.IsNullOrWhiteSpace(propertyToValidate))
            {
                validationErrors.Add(new ValidationResult(passingScoreLabelMustHaveANameAndCannotBeBlank));
            }
        }
    }
}
