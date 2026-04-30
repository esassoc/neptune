using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class MaintenanceRecordProjections
{
    public static readonly Expression<Func<MaintenanceRecord, MaintenanceRecordDetailDto>> AsDetailDto =
        x => new MaintenanceRecordDetailDto
        {
            MaintenanceRecordID = x.MaintenanceRecordID,
            FieldVisitID = x.FieldVisitID,
            TreatmentBMPID = x.TreatmentBMPID,
            TreatmentBMPTypeID = x.TreatmentBMPTypeID,
            MaintenanceRecordTypeID = x.MaintenanceRecordTypeID,
            // MaintenanceRecordTypeDisplayName resolved post-materialize from the static lookup.
            MaintenanceRecordDescription = x.MaintenanceRecordDescription,
            Observations = x.MaintenanceRecordObservations
                .OrderBy(o => o.CustomAttributeType.CustomAttributeTypeName)
                .Select(o => new MaintenanceRecordObservationDto
                {
                    MaintenanceRecordObservationID = o.MaintenanceRecordObservationID,
                    CustomAttributeTypeID = o.CustomAttributeTypeID,
                    CustomAttributeTypeName = o.CustomAttributeType.CustomAttributeTypeName,
                    Values = o.MaintenanceRecordObservationValues.Select(v => new MaintenanceRecordObservationValueDto
                    {
                        MaintenanceRecordObservationValueID = v.MaintenanceRecordObservationValueID,
                        ObservationValue = v.ObservationValue,
                    }).ToList(),
                }).ToList(),
        };
}
