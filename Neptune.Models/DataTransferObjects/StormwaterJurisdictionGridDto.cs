namespace Neptune.Models.DataTransferObjects
{
    public class StormwaterJurisdictionGridDto
    {
        public int StormwaterJurisdictionID { get; set; }
        public string StormwaterJurisdictionName { get; set; }
        public int OrganizationID { get; set; }
        public int StormwaterJurisdictionPublicBMPVisibilityTypeID { get; set; }
        public int StormwaterJurisdictionPublicWQMPVisibilityTypeID { get; set; }
        public string StormwaterJurisdictionPublicBMPVisibilityTypeName { get; set; }
        public string StormwaterJurisdictionPublicWQMPVisibilityTypeName { get; set; }
        public int NumberOfUsers { get; set; }
        public int NumberOfBMPs { get; set; }
    }
}