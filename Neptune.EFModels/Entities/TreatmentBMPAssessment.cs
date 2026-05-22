/*-----------------------------------------------------------------------
<copyright file="TreatmentBMPAssessment.cs" company="Tahoe Regional Planning Agency">
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

using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities
{
    public partial class TreatmentBMPAssessment
    {
        public bool CanEdit(Person currentPerson)
        {
            var canManageStormwaterJurisdiction = currentPerson.IsAssignedToStormwaterJurisdiction(TreatmentBMP.StormwaterJurisdictionID);
            return canManageStormwaterJurisdiction;
        }

        public string GetAssessmentStatus(TreatmentBMPType treatmentBMPType)
        {
            var completedObservationCount =
                treatmentBMPType.GetObservationTypes().Count(IsObservationComplete);
            var totalObservationCount = treatmentBMPType.GetObservationTypes().Count;
            return IsAssessmentComplete
                ? "Complete"
                : $"Incomplete ({completedObservationCount} of {totalObservationCount} observations complete)";
        }

        public string FormattedScore()
        {
            return AssessmentScore?.ToString("0.0") ?? "-";
        }

        public bool IsObservationComplete(TreatmentBMPAssessmentObservationType treatmentBMPAssessmentObservationType)
        {
            var treatmentBMPObservation = TreatmentBMPObservations.FirstOrDefault(x =>
                x.TreatmentBMPAssessmentObservationTypeID ==
                treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeID);

            return treatmentBMPObservation != null;
        }

        public Person GetFieldVisitPerson()
        {
            return FieldVisit.PerformedByPerson;
        }


        public DateTime GetAssessmentDate()
        {
            return FieldVisit.VisitDate;
        }

        public async Task DeleteFull(NeptuneDbContext dbContext)
        {
            await dbContext.TreatmentBMPAssessmentPhotos.Where(x => x.TreatmentBMPAssessmentID == TreatmentBMPAssessmentID)
                .ExecuteDeleteAsync();
            await dbContext.TreatmentBMPObservations.Where(x => x.TreatmentBMPAssessmentID == TreatmentBMPAssessmentID).ExecuteDeleteAsync();
            await dbContext.TreatmentBMPAssessments.Where(x => x.TreatmentBMPAssessmentID == TreatmentBMPAssessmentID)
                .ExecuteDeleteAsync();
        }

        public void CalculateIsAssessmentComplete()
        {
            IsAssessmentComplete = TreatmentBMP.TreatmentBMPType.GetObservationTypes().All(IsObservationComplete);
        }

        public void CalculateAssessmentScore(TreatmentBMPType treatmentBMPType, TreatmentBMP treatmentBMP)
        {
            if (!treatmentBMP.IsBenchmarkAndThresholdsComplete(treatmentBMPType))
            {
                return;
            }

            if (!IsAssessmentComplete)
            {
                return;
            }

            var observationTypesThatPotentiallyOverrideScore = treatmentBMPType.TreatmentBMPTypeAssessmentObservationTypes
                .Where(x => x.OverrideAssessmentScoreIfFailing)
                .Select(x => x.TreatmentBMPAssessmentObservationType)
                .ToList();

            double? score;
            var observations = TreatmentBMPObservations;
            if (observationTypesThatPotentiallyOverrideScore.Any(x =>
                {
                    var observation = observations.SingleOrDefault(y => y.TreatmentBMPAssessmentObservationType == x);
                    return observation?.OverrideScoreForFailingObservation() ?? false;
                }))
            {
                score = 0;
            }
            else if (treatmentBMPType.TreatmentBMPTypeAssessmentObservationTypes.All(x => x.OverrideAssessmentScoreIfFailing))
            {
                score = 5;
            }
            else
            {
                score = treatmentBMPType.TreatmentBMPTypeAssessmentObservationTypes
                    .Where(x => !x.OverrideAssessmentScoreIfFailing)
                    .Select(x => x.TreatmentBMPAssessmentObservationType)
                    .Sum(x =>
                    {
                        var observation = observations.SingleOrDefault(y =>
                            y.TreatmentBMPAssessmentObservationTypeID == x.TreatmentBMPAssessmentObservationTypeID);
                        if (observation == null)
                        {
                            throw new NullReferenceException(
                                $"Cannot find Treatment BMP observation type {x.TreatmentBMPAssessmentObservationTypeName}");
                        }
                        var observationScore = observation.CalculateObservationScore(treatmentBMP);
                        var observationWeight = Convert.ToDouble(treatmentBMPType
                            .GetTreatmentBMPTypeObservationType(x)
                            .AssessmentScoreWeight!.Value);
                        return observationScore * observationWeight;
                    });
            }

            AssessmentScore = score;
        }
    }
}
