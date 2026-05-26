using System.Linq.Expressions;
using Neptune.Common;
using Neptune.Models.DataTransferObjects;
using Neptune.Models.DataTransferObjects.ManagerDashboard;

namespace Neptune.EFModels.Entities;

public static class DelineationProjections
{
    // Manager Dashboard "Provisional BMP Delineations" grid. SQL-side projection so the geometry
    // area calculation and the BMP/jurisdiction join happen in the database. DelineationTypeName
    // is from a static lookup (DelineationType.AllLookupDictionary) and resolved post-materialize.
    public static readonly Expression<Func<Delineation, DelineationProvisionalGridDto>> AsProvisionalGridDto = x => new DelineationProvisionalGridDto
    {
        DelineationID = x.DelineationID,
        TreatmentBMPID = x.TreatmentBMPID,
        TreatmentBMPName = x.TreatmentBMP.TreatmentBMPName,
        TreatmentBMPTypeName = x.TreatmentBMP.TreatmentBMPType.TreatmentBMPTypeName,
        DelineationTypeID = x.DelineationTypeID,
        DelineationTypeName = null, // resolved post-materialize from the static lookup
        DelineationAreaInAcres = x.DelineationGeometry != null
            ? (double?)(x.DelineationGeometry.Area * Constants.SquareMetersToAcres)
            : null,
        DateLastModified = x.DateLastModified,
        DateLastVerified = x.DateLastVerified,
        StormwaterJurisdictionID = x.TreatmentBMP.StormwaterJurisdictionID,
        StormwaterJurisdictionName = x.TreatmentBMP.StormwaterJurisdiction.Organization.OrganizationName,
    };

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
