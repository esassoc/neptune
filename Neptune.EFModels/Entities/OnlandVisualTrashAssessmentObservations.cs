using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Common.GeoSpatial;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities
{
    public static class OnlandVisualTrashAssessmentObservations
    {
        private static IQueryable<OnlandVisualTrashAssessmentObservation> GetImpl(NeptuneDbContext dbContext)
        {
            return dbContext.OnlandVisualTrashAssessmentObservations
                    .Include(x => x.OnlandVisualTrashAssessment)
                    .ThenInclude(x => x.OnlandVisualTrashAssessmentArea)
                    .Include(x => x.OnlandVisualTrashAssessmentObservationPhotos)
                    .ThenInclude(x => x.FileResource)
                ;
        }

        public static OnlandVisualTrashAssessmentObservation GetByIDWithChangeTracking(NeptuneDbContext dbContext, int onlandVisualTrashAssessmentObservationID)
        {
            var onlandVisualTrashAssessmentObservation = GetImpl(dbContext)
                .SingleOrDefault(x => x.OnlandVisualTrashAssessmentObservationID == onlandVisualTrashAssessmentObservationID);
            Check.RequireNotNull(onlandVisualTrashAssessmentObservation, $"OnlandVisualTrashAssessmentObservation with ID {onlandVisualTrashAssessmentObservationID} not found!");
            return onlandVisualTrashAssessmentObservation;
        }

        public static OnlandVisualTrashAssessmentObservation GetByIDWithChangeTracking(NeptuneDbContext dbContext, OnlandVisualTrashAssessmentObservationPrimaryKey onlandVisualTrashAssessmentObservationPrimaryKey)
        {
            return GetByIDWithChangeTracking(dbContext, onlandVisualTrashAssessmentObservationPrimaryKey.PrimaryKeyValue);
        }

        public static OnlandVisualTrashAssessmentObservation GetByID(NeptuneDbContext dbContext, int onlandVisualTrashAssessmentObservationID)
        {
            var onlandVisualTrashAssessmentObservation = GetImpl(dbContext).AsNoTracking()
                .SingleOrDefault(x => x.OnlandVisualTrashAssessmentObservationID == onlandVisualTrashAssessmentObservationID);
            Check.RequireNotNull(onlandVisualTrashAssessmentObservation, $"OnlandVisualTrashAssessmentObservation with ID {onlandVisualTrashAssessmentObservationID} not found!");
            return onlandVisualTrashAssessmentObservation;
        }

        public static OnlandVisualTrashAssessmentObservation GetByID(NeptuneDbContext dbContext, OnlandVisualTrashAssessmentObservationPrimaryKey onlandVisualTrashAssessmentObservationPrimaryKey)
        {
            return GetByID(dbContext, onlandVisualTrashAssessmentObservationPrimaryKey.PrimaryKeyValue);
        }

        public static List<OnlandVisualTrashAssessmentObservation> ListByOnlandVisualTrashAssessmentID(NeptuneDbContext dbContext, int onlandVisualTrashAssessmentID)
        {
            return GetImpl(dbContext).AsNoTracking().Where(x => x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID).OrderBy(x => x.ObservationDatetime).ToList();
        }

        public static async Task Update(NeptuneDbContext dbContext, int onlandVisualTrashAssessmentID,
        List<OnlandVisualTrashAssessmentObservationWithPhotoDto> onlandVisualTrashAssessmentObservationUpsertDtos)
        {
            await dbContext.OnlandVisualTrashAssessmentObservationPhotos
                .Include(x => x.OnlandVisualTrashAssessmentObservation).Where(x =>
                    x.OnlandVisualTrashAssessmentObservation.OnlandVisualTrashAssessmentID ==
                    onlandVisualTrashAssessmentID).ExecuteDeleteAsync();
            await dbContext.OnlandVisualTrashAssessmentObservations.Where(x =>
                x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID).ExecuteDeleteAsync();

            foreach (var onlandVisualTrashAssessmentObservationUpsertDto in onlandVisualTrashAssessmentObservationUpsertDtos)
            {
                var locationPoint4326FromLatLong = GeometryHelper.CreateLocationPoint4326FromLatLong(onlandVisualTrashAssessmentObservationUpsertDto.Latitude, onlandVisualTrashAssessmentObservationUpsertDto.Longitude);
                var onlandVisualTrashAssessmentObservation = new OnlandVisualTrashAssessmentObservation()
                {
                    OnlandVisualTrashAssessmentID = onlandVisualTrashAssessmentID,
                    Note = onlandVisualTrashAssessmentObservationUpsertDto.Note,
                    ObservationDatetime = onlandVisualTrashAssessmentObservationUpsertDto.ObservationDatetime ?? DateTime.UtcNow,
                    LocationPoint4326 = locationPoint4326FromLatLong,
                    LocationPoint = locationPoint4326FromLatLong.ProjectTo2771(),
                };


                if (onlandVisualTrashAssessmentObservationUpsertDto.FileResourceID != null)
                {
                    var onlandVisualTrashAssessmentObservationPhoto = new OnlandVisualTrashAssessmentObservationPhoto
                        {
                            FileResourceID = (int)onlandVisualTrashAssessmentObservationUpsertDto.FileResourceID
                        };

                    onlandVisualTrashAssessmentObservation.OnlandVisualTrashAssessmentObservationPhotos = [onlandVisualTrashAssessmentObservationPhoto];
                }

                dbContext.OnlandVisualTrashAssessmentObservations.Add(onlandVisualTrashAssessmentObservation);
            }

            await dbContext.OnlandVisualTrashAssessmentObservationPhotoStagings
                .Where(x => x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID).ExecuteDeleteAsync();

            await dbContext.SaveChangesAsync();

            // Return-to-Edit path: if the user edited observations on the area's backing
            // assessment after it was already finalized, recompute the area's stored
            // TransectLine from the new geometry. Mirrors the logic in
            // OnlandVisualTrashAssessments.Update (the finalize path) so both save paths
            // keep the area's transect in sync with the backing assessment's observations.
            // Repeat assessments (IsTransectBackingAssessment == false) don't hit this branch
            // and the area's transect stays anchored to the original backing as intended.
            var assessment = await dbContext.OnlandVisualTrashAssessments
                .Include(x => x.OnlandVisualTrashAssessmentArea)
                .Include(x => x.OnlandVisualTrashAssessmentObservations)
                .SingleOrDefaultAsync(x => x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID);

            if (assessment?.OnlandVisualTrashAssessmentArea == null) return;

            var wasNeverSet = assessment.OnlandVisualTrashAssessmentArea.TransectLine == null;
            var isBacking = assessment.IsTransectBackingAssessment || wasNeverSet;

            if (isBacking && assessment.OnlandVisualTrashAssessmentObservations.Count >= 2)
            {
                var transect = OnlandVisualTrashAssessments.GetTransectLine(assessment.OnlandVisualTrashAssessmentObservations);
                if (transect != null)
                {
                    assessment.OnlandVisualTrashAssessmentArea.TransectLine = transect;
                    assessment.OnlandVisualTrashAssessmentArea.TransectLine4326 = transect.ProjectTo4326();
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
