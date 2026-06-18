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

    public static FieldVisit GetByID(NeptuneDbContext dbContext, int fieldVisitID)
    {
        var fieldVisit = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.FieldVisitID == fieldVisitID);
        Check.RequireNotNull(fieldVisit,
            $"FieldVisit with ID {fieldVisitID} not found!");
        return fieldVisit;
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
        // NPT-984: check for the in-progress visit with AsNoTracking — we only need the ID and
        // the routing decision. The earlier `GetInProgressForTreatmentBMPIfAny` call pulled the
        // FieldVisit with its full include tree (TreatmentBMP + assessments + photos + ...) into
        // the change tracker, and EF's NavigationFixer then choked when we AddAsync'd the new
        // FieldVisit below — the existing tracked TreatmentBMP entity carried a navigation
        // collection that fixup tried to re-link, conceptually nulling the existing FieldVisit's
        // non-nullable TreatmentBMPID FK and surfacing "association ... has been severed".
        var existingInProgress = await dbContext.FieldVisits.AsNoTracking()
            .Where(x => x.TreatmentBMPID == treatmentBMPID
                        && x.FieldVisitStatusID == FieldVisitStatus.InProgress.FieldVisitStatusID)
            .Select(x => new { x.FieldVisitID })
            .SingleOrDefaultAsync();

        if (existingInProgress != null && createDto.ContinueExistingInProgress == true)
        {
            return GetByIDAsDto(dbContext, existingInProgress.FieldVisitID);
        }

        if (existingInProgress != null && createDto.ContinueExistingInProgress == false)
        {
            // The FieldVisit table has a unique filtered index
            // (CK_AtMostOneFieldVisitMayBeInProgressAtAnyTimePerBMP — at most one InProgress
            // visit per BMP). Flush the Unresolved write before the AddAsync below so the
            // unique slot is free. Use ExecuteUpdateAsync so the change tracker stays clean —
            // round 5 originally loaded the entity and called SaveChangesAsync here, which is
            // what dragged the loaded graph into the tracker and broke navigation fixup.
            await dbContext.FieldVisits
                .Where(x => x.FieldVisitID == existingInProgress.FieldVisitID)
                .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.FieldVisitStatusID, FieldVisitStatus.Unresolved.FieldVisitStatusID));
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

    // Manager Dashboard: bulk-verify a set of field visits. Jurisdiction-scoped — silently
    // drops IDs the caller can't view (no per-row 403 noise). Returns the count actually
    // verified so the SPA toast can say "X verified" when the user submitted X+Y.
    public static async Task<int> BulkMarkAsVerifiedAsync(NeptuneDbContext dbContext, IList<int> fieldVisitIDs, Person currentPerson)
    {
        if (fieldVisitIDs == null || fieldVisitIDs.Count == 0) return 0;

        // ToList() materializes — EF needs a List<int> (not raw IEnumerable<int>) to translate
        // `.Contains(...)` into a SQL IN clause reliably.
        var viewableJurisdictionIDs = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForBMPs(dbContext, currentPerson).ToList();
        var fieldVisits = await dbContext.FieldVisits
            .Include(x => x.TreatmentBMP)
            .Where(x => fieldVisitIDs.Contains(x.FieldVisitID)
                && viewableJurisdictionIDs.Contains(x.TreatmentBMP.StormwaterJurisdictionID))
            .ToListAsync();
        foreach (var fieldVisit in fieldVisits)
        {
            fieldVisit.VerifyFieldVisit();
        }
        await dbContext.SaveChangesAsync();
        return fieldVisits.Count;
    }
}
