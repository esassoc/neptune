using System.Linq.Expressions;
using Neptune.Common;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class DelineationProjections
{
    // Discrepancies grid: SQL-side projection with JOINs for non-enum lookups
    // (TreatmentBMPType.TreatmentBMPTypeName, Organization.OrganizationName). The
    // DelineationType display name is enum-style — projection emits the ID and the
    // caller fills the name post-materialize via DelineationType.AllLookupDictionary.
    public static readonly Expression<Func<Delineation, DelineationReconciliationDiscrepancyGridDto>> AsDiscrepancyGridDto = x => new DelineationReconciliationDiscrepancyGridDto
    {
        DelineationID = x.DelineationID,
        TreatmentBMPID = x.TreatmentBMPID,
        TreatmentBMPName = x.TreatmentBMP.TreatmentBMPName,
        TreatmentBMPTypeName = x.TreatmentBMP.TreatmentBMPType.TreatmentBMPTypeName,
        DelineationTypeID = x.DelineationTypeID,
        DelineationTypeName = null,
        AreaInAcres = x.DelineationGeometry.Area * Constants.SquareMetersToAcres,
        DateLastModified = x.DateLastModified,
        DateLastVerified = x.DateLastVerified,
        StormwaterJurisdictionID = x.TreatmentBMP.StormwaterJurisdictionID,
        StormwaterJurisdictionName = x.TreatmentBMP.StormwaterJurisdiction.Organization.OrganizationName,
    };

    public static readonly Expression<Func<Delineation, DelineationReconciliationOverlapGridDto>> AsOverlapGridDto = x => new DelineationReconciliationOverlapGridDto
    {
        DelineationID = x.DelineationID,
        TreatmentBMPID = x.TreatmentBMPID,
        TreatmentBMPName = x.TreatmentBMP.TreatmentBMPName,
        TreatmentBMPTypeName = x.TreatmentBMP.TreatmentBMPType.TreatmentBMPTypeName,
        AreaInAcres = x.DelineationGeometry.Area * Constants.SquareMetersToAcres,
        DateLastModified = x.DateLastModified,
        DateLastVerified = x.DateLastVerified,
        StormwaterJurisdictionID = x.TreatmentBMP.StormwaterJurisdictionID,
        StormwaterJurisdictionName = x.TreatmentBMP.StormwaterJurisdiction.Organization.OrganizationName,
        AreaOfOverlapInAcres = x.DelineationOverlapDelineations.Sum(y => y.OverlappingGeometry.Area) * Constants.SquareMetersToAcres,
        // NPT-1064: legacy MVC grid linked every entry to the parent BMP. Link to each
        // overlapping BMP's own detail page (one row per overlap, ordered by name).
        OverlappingDelineations = x.DelineationOverlapDelineations
            .OrderBy(y => y.OverlappingDelineation.TreatmentBMP.TreatmentBMPName)
            .Select(y => new OverlappingDelineationLinkDto
            {
                TreatmentBMPID = y.OverlappingDelineation.TreatmentBMPID,
                TreatmentBMPName = y.OverlappingDelineation.TreatmentBMP.TreatmentBMPName,
            }).ToList(),
    };
}
