using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class StormwaterJurisdictionExtensionMethods
{
    public static StormwaterJurisdictionDisplayDto AsDisplayDto(this StormwaterJurisdiction stormwaterJurisdiction)
    {
        var treatmentBMPSimpleDto = new StormwaterJurisdictionDisplayDto()
        {
            StormwaterJurisdictionID = stormwaterJurisdiction.StormwaterJurisdictionID,
            StormwaterJurisdictionName = stormwaterJurisdiction.Organization.OrganizationName
        };
        return treatmentBMPSimpleDto;
    }

    public static StormwaterJurisdictionGridDto AsGridDto(this StormwaterJurisdiction stormwaterJurisdiction)
    {
        var dto = new StormwaterJurisdictionGridDto()
        {
            StormwaterJurisdictionID = stormwaterJurisdiction.StormwaterJurisdictionID,
            StormwaterJurisdictionName = stormwaterJurisdiction.Organization.OrganizationName,
            OrganizationID = stormwaterJurisdiction.OrganizationID,
            StormwaterJurisdictionPublicBMPVisibilityTypeID = stormwaterJurisdiction.StormwaterJurisdictionPublicBMPVisibilityTypeID,
            StormwaterJurisdictionPublicWQMPVisibilityTypeID = stormwaterJurisdiction.StormwaterJurisdictionPublicWQMPVisibilityTypeID,
            StormwaterJurisdictionPublicBMPVisibilityTypeName = stormwaterJurisdiction.StormwaterJurisdictionPublicBMPVisibilityType.StormwaterJurisdictionPublicBMPVisibilityTypeDisplayName,
            StormwaterJurisdictionPublicWQMPVisibilityTypeName = stormwaterJurisdiction.StormwaterJurisdictionPublicWQMPVisibilityType.StormwaterJurisdictionPublicWQMPVisibilityTypeDisplayName,
        };
        return dto;
    }
}