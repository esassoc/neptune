using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class RegionalSubbasinRevisionRequestDtoProjections
{
    public static readonly Expression<Func<RegionalSubbasinRevisionRequest, RegionalSubbasinRevisionRequestDto>> AsDto = x => new RegionalSubbasinRevisionRequestDto
    {
        RegionalSubbasinRevisionRequestID = x.RegionalSubbasinRevisionRequestID,
        RegionalSubbasinRevisionRequestStatusID = x.RegionalSubbasinRevisionRequestStatusID,
        TreatmentBMPID = x.TreatmentBMPID,
        TreatmentBMPName = x.TreatmentBMP.TreatmentBMPName,
        RequestPersonID = x.RequestPersonID,
        RequestPersonName = x.RequestPerson.LastName + ", " + x.RequestPerson.FirstName,
        RequestDate = x.RequestDate,
        ClosedByPersonID = x.ClosedByPersonID,
        ClosedByPersonName = x.ClosedByPerson != null ? x.ClosedByPerson.LastName + ", " + x.ClosedByPerson.FirstName : null,
        ClosedDate = x.ClosedDate,
        Notes = x.Notes,
        CloseNotes = x.CloseNotes,
        // RegionalSubbasinRevisionRequestStatusDisplayName resolved post-materialize via AllLookupDictionary
        // GeometryGeoJson populated post-materialize (geometry needs runtime projection to 4326)
    };

    public static RegionalSubbasinRevisionRequestDto ResolveLookups(this RegionalSubbasinRevisionRequestDto dto)
    {
        dto.RegionalSubbasinRevisionRequestStatusDisplayName = RegionalSubbasinRevisionRequestStatus.AllLookupDictionary[dto.RegionalSubbasinRevisionRequestStatusID].RegionalSubbasinRevisionRequestStatusDisplayName;
        return dto;
    }
}
