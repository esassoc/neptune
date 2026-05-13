using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects;

public class MaintenanceRecordObservationDto
{
    public int MaintenanceRecordObservationID { get; set; }
    public int CustomAttributeTypeID { get; set; }
    public string? CustomAttributeTypeName { get; set; }
    public List<MaintenanceRecordObservationValueDto> Values { get; set; } = new();
}
