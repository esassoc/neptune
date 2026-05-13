using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects;

public class MaintenanceRecordObservationUpsertDto
{
    public int CustomAttributeTypeID { get; set; }
    public List<string?> Values { get; set; } = new();
}
