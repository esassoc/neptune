using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPObservations
{
    private static IQueryable<TreatmentBMPObservation> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.TreatmentBMPObservations;
    }

    public static TreatmentBMPObservation GetByIDWithChangeTracking(NeptuneDbContext dbContext, int treatmentBMPObservationID)
    {
        var treatmentBMPObservation = GetImpl(dbContext)
            .SingleOrDefault(x => x.TreatmentBMPObservationID == treatmentBMPObservationID);
        Check.RequireNotNull(treatmentBMPObservation, $"TreatmentBMPObservation with ID {treatmentBMPObservationID} not found!");
        return treatmentBMPObservation;
    }

    public static TreatmentBMPObservation GetByID(NeptuneDbContext dbContext, int treatmentBMPObservationID)
    {
        var treatmentBMPObservation = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.TreatmentBMPObservationID == treatmentBMPObservationID);
        Check.RequireNotNull(treatmentBMPObservation, $"TreatmentBMPObservation with ID {treatmentBMPObservationID} not found!");
        return treatmentBMPObservation;
    }
}