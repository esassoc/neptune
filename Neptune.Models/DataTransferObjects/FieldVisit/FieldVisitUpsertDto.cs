using System;

namespace Neptune.Models.DataTransferObjects;

public class FieldVisitUpsertDto
{
    public DateTime VisitDate { get; set; }
    public int FieldVisitTypeID { get; set; }
}
