using Neptune.Common;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class RegionalSubbasinExtensionMethods
{

    public static RegionalSubbasinDto AsDto(this RegionalSubbasin entity)
    {
        return new RegionalSubbasinDto
        {
            RegionalSubbasinID = entity.RegionalSubbasinID,
            OCSurveyCatchmentID = entity.OCSurveyCatchmentID,
            OCSurveyDownstreamCatchmentID = entity.OCSurveyDownstreamCatchmentID,
            DownstreamRegionalSubbasinID = entity.OCSurveyDownstreamCatchment?.RegionalSubbasinID,
            Watershed = entity.Watershed,
            DrainID = entity.DrainID,
            DisplayName = entity.GetDisplayName(),
            Area = entity.CatchmentGeometry?.Area * Constants.SquareMetersToAcres
        };
    }
}