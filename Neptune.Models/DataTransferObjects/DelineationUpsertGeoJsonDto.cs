namespace Neptune.Models.DataTransferObjects
{
    public class DelineationUpsertGeoJsonDto
    {
        public int DelineationTypeID { get; set; }
        public string? GeoJson { get; set; }
    }
}
