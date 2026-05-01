using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects;

public class MaintenanceRecordUpsertDto
{
    public int? MaintenanceRecordTypeID { get; set; }
    public string? MaintenanceRecordDescription { get; set; }
    public List<MaintenanceRecordObservationUpsertDto> Observations { get; set; } = new();
}
