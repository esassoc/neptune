namespace Neptune.Models.DataTransferObjects;

public class DataHubLastUpdatedDto
{
    public DateTime? Parcels { get; set; }
    public DateTime? RegionalSubbasins { get; set; }
    public DateTime? HRUCharacteristics { get; set; }
    public DateTime? ModelBasins { get; set; }
    public DateTime? PrecipitationZones { get; set; }
}
