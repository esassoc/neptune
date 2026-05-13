using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects;

public class MaintenanceRecordDetailDto
{
    public int MaintenanceRecordID { get; set; }
    public int FieldVisitID { get; set; }
    public int TreatmentBMPID { get; set; }
    public int TreatmentBMPTypeID { get; set; }
    public int? MaintenanceRecordTypeID { get; set; }
    public string? MaintenanceRecordTypeDisplayName { get; set; }
    public string? MaintenanceRecordDescription { get; set; }
    public List<MaintenanceRecordObservationDto> Observations { get; set; } = new();
}
