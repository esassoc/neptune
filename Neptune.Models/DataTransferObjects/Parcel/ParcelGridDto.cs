namespace Neptune.Models.DataTransferObjects;

public class ParcelGridDto
{
    public int ParcelID { get; set; }
    public string ParcelNumber { get; set; }
    public string ParcelAddress { get; set; }
    public string ParcelCityState { get; set; }
    public string ParcelZipCode { get; set; }
    public double ParcelAreaInAcres { get; set; }
}
