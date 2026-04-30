using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

public partial class FieldVisit
{
    public TreatmentBMPAssessment? GetAssessmentByType(TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum)
    {
        return TreatmentBMPAssessments.SingleOrDefault(x => x.TreatmentBMPAssessmentTypeID == (int)treatmentBMPAssessmentTypeEnum);
    }

    public TreatmentBMPAssessment? GetInitialAssessment()
    {
        return GetAssessmentByType(TreatmentBMPAssessmentTypeEnum.Initial);
    }

    public TreatmentBMPAssessment? GetPostMaintenanceAssessment()
    {
        return GetAssessmentByType(TreatmentBMPAssessmentTypeEnum.PostMaintenance);
    }

    public void VerifyFieldVisit()
    {
        IsFieldVisitVerified = true;
        FieldVisitStatusID = (int)FieldVisitStatusEnum.Complete;
    }

    public void MarkFieldVisitAsProvisional()
    {
        IsFieldVisitVerified = false;
    }

    public void ReturnFieldVisitToEdit()
    {
        IsFieldVisitVerified = false;
        FieldVisitStatusID = (int)FieldVisitStatusEnum.ReturnedToEdit;
    }

    public void MarkFieldVisitAsProvisionalIfNonManager(Person person)
    {
        var canManageJurisdiction = person.IsManagerOrAdmin()
            && person.IsAssignedToStormwaterJurisdiction(TreatmentBMP.StormwaterJurisdictionID);
        if (!canManageJurisdiction)
        {
            IsFieldVisitVerified = false;
        }
    }

    public async Task DeleteFull(NeptuneDbContext dbContext)
    {
        await dbContext.MaintenanceRecordObservationValues
            .Include(x => x.MaintenanceRecordObservation)
            .ThenInclude(x => x.MaintenanceRecord)
            .Where(x => x.MaintenanceRecordObservation.MaintenanceRecord.FieldVisitID == FieldVisitID)
            .ExecuteDeleteAsync();
        await dbContext.MaintenanceRecordObservations
            .Include(x => x.MaintenanceRecord)
            .Where(x => x.MaintenanceRecord.FieldVisitID == FieldVisitID).ExecuteDeleteAsync();
        await dbContext.MaintenanceRecords.Where(x => x.FieldVisitID == FieldVisitID).ExecuteDeleteAsync();
        await dbContext.TreatmentBMPAssessmentPhotos
            .Include(x => x.TreatmentBMPAssessment)
            .Where(x => x.TreatmentBMPAssessment.FieldVisitID == FieldVisitID)
            .ExecuteDeleteAsync();
        await dbContext.TreatmentBMPObservations
            .Include(x => x.TreatmentBMPAssessment)
            .Where(x => x.TreatmentBMPAssessment.FieldVisitID == FieldVisitID)
            .ExecuteDeleteAsync();
        await dbContext.TreatmentBMPAssessments
            .Where(x => x.FieldVisitID == FieldVisitID)
            .ExecuteDeleteAsync();
        await dbContext.FieldVisits.Where(x => x.FieldVisitID == FieldVisitID).ExecuteDeleteAsync();
    }
}