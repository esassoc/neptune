using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class FieldVisits
{
    public static IQueryable<FieldVisit> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.FieldVisits
            .Include(x => x.TreatmentBMP)
            .ThenInclude(x => x.InventoryVerifiedByPerson)
            .Include(x => x.TreatmentBMP)
            .ThenInclude(x => x.TreatmentBMPType)
            .ThenInclude(x => x.TreatmentBMPTypeCustomAttributeTypes)
            .ThenInclude(x => x.CustomAttributeType)
            .Include(x => x.MaintenanceRecord)
            .Include(x => x.TreatmentBMP)
            .ThenInclude(x => x.TreatmentBMPImages)
            .ThenInclude(x => x.FileResource)
            .Include(x => x.TreatmentBMPAssessments)
            .ThenInclude(x => x.TreatmentBMPAssessmentPhotos)
            .ThenInclude(x => x.FileResource)
            .Include(x => x.PerformedByPerson)
            ;
    }

    public static FieldVisit GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        int fieldVisitID)
    {
        var fieldVisit = GetImpl(dbContext)
            .SingleOrDefault(x => x.FieldVisitID == fieldVisitID);
        Check.RequireNotNull(fieldVisit,
            $"FieldVisit with ID {fieldVisitID} not found!");
        return fieldVisit;
    }

    public static FieldVisit GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        FieldVisitPrimaryKey fieldVisitPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, fieldVisitPrimaryKey.PrimaryKeyValue);
    }

    public static FieldVisit GetByID(NeptuneDbContext dbContext, int fieldVisitID)
    {
        var fieldVisit = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.FieldVisitID == fieldVisitID);
        Check.RequireNotNull(fieldVisit,
            $"FieldVisit with ID {fieldVisitID} not found!");
        return fieldVisit;
    }

    public static FieldVisit GetByID(NeptuneDbContext dbContext,
        FieldVisitPrimaryKey fieldVisitPrimaryKey)
    {
        return GetByID(dbContext, fieldVisitPrimaryKey.PrimaryKeyValue);
    }

    public static List<FieldVisit> ListByTreatmentBMPID(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        return GetImpl(dbContext).AsNoTracking()
            .Where(x => x.TreatmentBMPID == treatmentBMPID).ToList();
    }

    public static FieldVisit? GetInProgressForTreatmentBMPIfAny(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        return GetImpl(dbContext).SingleOrDefault(x =>
            x.TreatmentBMPID == treatmentBMPID &&
            x.FieldVisitStatusID == FieldVisitStatus.InProgress.FieldVisitStatusID);
    }

    public static List<FieldVisit> ListByFieldVisitIDList(NeptuneDbContext dbContext, List<int> fieldVisitIDList)
    {
        return GetImpl(dbContext).AsNoTracking()
            .Where(x => fieldVisitIDList.Contains(x.FieldVisitID)).ToList();
    }

    public static List<FieldVisit> ListByFieldVisitIDListWithChangeTracking(NeptuneDbContext dbContext, List<int> fieldVisitIDList)
    {
        return GetImpl(dbContext).Where(x => fieldVisitIDList.Contains(x.FieldVisitID)).ToList();
    }

    public static FieldVisitDto? GetInProgressForTreatmentBMPIfAnyAsDto(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        var inProgress = dbContext.vFieldVisitDetaileds.AsNoTracking().FirstOrDefault(x =>
            x.TreatmentBMPID == treatmentBMPID &&
            x.FieldVisitStatusID == FieldVisitStatus.InProgress.FieldVisitStatusID);
        return inProgress?.AsDto();
    }

    public static FieldVisitDto GetByIDAsDto(NeptuneDbContext dbContext, int fieldVisitID)
    {
        var entity = dbContext.vFieldVisitDetaileds.AsNoTracking()
            .Single(x => x.FieldVisitID == fieldVisitID);
        return entity.AsDto();
    }

    public static FieldVisitWorkflowDto GetByIDAsWorkflowDto(NeptuneDbContext dbContext, int fieldVisitID)
    {
        var entity = dbContext.vFieldVisitDetaileds.AsNoTracking()
            .Single(x => x.FieldVisitID == fieldVisitID);
        return entity.AsWorkflowDto();
    }

    public static List<FieldVisitDto> ListAsDtoForJurisdictions(NeptuneDbContext dbContext, IEnumerable<int> stormwaterJurisdictionIDsPersonCanView)
    {
        return dbContext.vFieldVisitDetaileds.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID))
            .OrderByDescending(x => x.VisitDate)
            .ToList()
            .Select(x => x.AsDto())
            .ToList();
    }

    public static async Task<FieldVisitDto> CreateAsync(NeptuneDbContext dbContext, int treatmentBMPID, FieldVisitCreateDto createDto, int performerPersonID)
    {
        var existing = GetInProgressForTreatmentBMPIfAny(dbContext, treatmentBMPID);
        if (existing != null && createDto.ContinueExistingInProgress == true)
        {
            return GetByIDAsDto(dbContext, existing.FieldVisitID);
        }

        if (existing != null && createDto.ContinueExistingInProgress == false)
        {
            existing.FieldVisitStatusID = FieldVisitStatus.Unresolved.FieldVisitStatusID;
        }

        var fieldVisit = new FieldVisit
        {
            TreatmentBMPID = treatmentBMPID,
            FieldVisitStatusID = FieldVisitStatus.InProgress.FieldVisitStatusID,
            PerformedByPersonID = performerPersonID,
            VisitDate = createDto.VisitDate.ToUniversalTime(),
            FieldVisitTypeID = createDto.FieldVisitTypeID,
            InventoryUpdated = false,
            IsFieldVisitVerified = false,
        };
        await dbContext.FieldVisits.AddAsync(fieldVisit);
        await dbContext.SaveChangesAsync();

        return GetByIDAsDto(dbContext, fieldVisit.FieldVisitID);
    }

    public static async Task<FieldVisitDto> UpdateDateAndTypeAsync(NeptuneDbContext dbContext, int fieldVisitID, FieldVisitUpsertDto upsertDto)
    {
        var fieldVisit = GetByIDWithChangeTracking(dbContext, fieldVisitID);
        fieldVisit.VisitDate = upsertDto.VisitDate.ToUniversalTime();
        fieldVisit.FieldVisitTypeID = upsertDto.FieldVisitTypeID;
        await dbContext.SaveChangesAsync();
        return GetByIDAsDto(dbContext, fieldVisitID);
    }

    public static async Task<FieldVisitDto> UpdateInventoryUpdatedAsync(NeptuneDbContext dbContext, int fieldVisitID, bool inventoryUpdated)
    {
        var fieldVisit = GetByIDWithChangeTracking(dbContext, fieldVisitID);
        fieldVisit.InventoryUpdated = inventoryUpdated;
        await dbContext.SaveChangesAsync();
        return GetByIDAsDto(dbContext, fieldVisitID);
    }

    public static async Task<FieldVisitDto> VerifyAsync(NeptuneDbContext dbContext, int fieldVisitID)
    {
        var fieldVisit = GetByIDWithChangeTracking(dbContext, fieldVisitID);
        fieldVisit.VerifyFieldVisit();
        await dbContext.SaveChangesAsync();
        return GetByIDAsDto(dbContext, fieldVisitID);
    }

    public static async Task<FieldVisitDto> MarkProvisionalAsync(NeptuneDbContext dbContext, int fieldVisitID)
    {
        var fieldVisit = GetByIDWithChangeTracking(dbContext, fieldVisitID);
        fieldVisit.MarkFieldVisitAsProvisional();
        await dbContext.SaveChangesAsync();
        return GetByIDAsDto(dbContext, fieldVisitID);
    }

    public static async Task<FieldVisitDto> ReturnToEditAsync(NeptuneDbContext dbContext, int fieldVisitID)
    {
        var fieldVisit = GetByIDWithChangeTracking(dbContext, fieldVisitID);
        fieldVisit.ReturnFieldVisitToEdit();
        await dbContext.SaveChangesAsync();
        return GetByIDAsDto(dbContext, fieldVisitID);
    }

    public static async Task<FieldVisitDto> FinalizeAsync(NeptuneDbContext dbContext, int fieldVisitID)
    {
        var fieldVisit = GetByIDWithChangeTracking(dbContext, fieldVisitID);
        fieldVisit.FieldVisitStatusID = (int)FieldVisitStatusEnum.Complete;
        await dbContext.SaveChangesAsync();
        return GetByIDAsDto(dbContext, fieldVisitID);
    }

    public static async Task DeleteAsync(NeptuneDbContext dbContext, int fieldVisitID)
    {
        var fieldVisit = GetByIDWithChangeTracking(dbContext, fieldVisitID);
        await fieldVisit.DeleteFull(dbContext);
    }
}
