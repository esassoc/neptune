using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPTypeCustomAttributeTypes
{
    public static IQueryable<TreatmentBMPTypeCustomAttributeType> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.TreatmentBMPTypeCustomAttributeTypes.Include(x => x.CustomAttributeType);
    }

    public static TreatmentBMPTypeCustomAttributeType GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        int treatmentBMPTypeCustomAttributeTypeID)
    {
        var treatmentBMPTypeCustomAttributeType = GetImpl(dbContext)
            .SingleOrDefault(x => x.TreatmentBMPTypeCustomAttributeTypeID == treatmentBMPTypeCustomAttributeTypeID);
        Check.RequireNotNull(treatmentBMPTypeCustomAttributeType,
            $"TreatmentBMPTypeCustomAttributeType with ID {treatmentBMPTypeCustomAttributeTypeID} not found!");
        return treatmentBMPTypeCustomAttributeType;
    }

    public static TreatmentBMPTypeCustomAttributeType GetByID(NeptuneDbContext dbContext, int treatmentBMPTypeCustomAttributeTypeID)
    {
        var treatmentBMPTypeCustomAttributeType = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.TreatmentBMPTypeCustomAttributeTypeID == treatmentBMPTypeCustomAttributeTypeID);
        Check.RequireNotNull(treatmentBMPTypeCustomAttributeType,
            $"TreatmentBMPTypeCustomAttributeType with ID {treatmentBMPTypeCustomAttributeTypeID} not found!");
        return treatmentBMPTypeCustomAttributeType;
    }

    public static TreatmentBMPTypeCustomAttributeTypeDto GetByIDAsDto(NeptuneDbContext dbContext, int treatmentBMPTypeCustomAttributeTypeID)
    {
        return GetByID(dbContext, treatmentBMPTypeCustomAttributeTypeID).AsDto();
    }

    public static List<TreatmentBMPTypeCustomAttributeType> List(NeptuneDbContext dbContext)
    {
        return GetImpl(dbContext).AsNoTracking().ToList();
    }

    public static List<TreatmentBMPTypeCustomAttributeTypeDto> ListAsDto(NeptuneDbContext dbContext)
    {
        return List(dbContext).Select(x => x.AsDto()).ToList();
    }

    public static List<TreatmentBMPTypeCustomAttributeTypeDto> ListByTreatmentBMPTypeAsDto(NeptuneDbContext dbContext, int treatmentBMPTypeID)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x => x.TreatmentBMPTypeID == treatmentBMPTypeID).ToList().Select(x => x.AsDto()).ToList();
    }

    public static List<TreatmentBMPTypeCustomAttributeTypeDto> GetByCustomAttributeTypePurposeAsDto(NeptuneDbContext dbContext, int treatmentBMPTypeCustomAttributeTypePurposeID)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x =>
                x.CustomAttributeType.CustomAttributeTypePurposeID == treatmentBMPTypeCustomAttributeTypePurposeID)
            .Select(x => x.AsDto())
            .ToList();
    }
}