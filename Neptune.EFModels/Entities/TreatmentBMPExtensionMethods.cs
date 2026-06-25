using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPExtensionMethods
{
    public static TreatmentBMPDisplayDto AsDisplayDto(this TreatmentBMP treatmentBMP, vTreatmentBMPModelingAttribute? treatmentBMPModelingAttribute)
    {
        var treatmentBmpDisplayDto = new TreatmentBMPDisplayDto()
        {
            TreatmentBMPID = treatmentBMP.TreatmentBMPID,
            DisplayName = treatmentBMP.TreatmentBMPName,
            TreatmentBMPTypeID = treatmentBMP.TreatmentBMPTypeID,
            TreatmentBMPTypeName = treatmentBMP.TreatmentBMPType.TreatmentBMPTypeName,
            ProjectID = treatmentBMP.ProjectID,
            InventoryIsVerified = treatmentBMP.InventoryIsVerified,
            Longitude = treatmentBMP.LocationPoint4326.Coordinate.X,
            Latitude = treatmentBMP.LocationPoint4326.Coordinate.Y,
            TreatmentBMPName = treatmentBMP.TreatmentBMPName,
            TreatmentBMPModelingAttribute = treatmentBMPModelingAttribute != null ? treatmentBMPModelingAttribute.AsDto() : null,
            CustomAttributes = treatmentBMP.CustomAttributes?.Select(x => x.AsUpsertDto()).ToList(),
            IsFullyParameterized = treatmentBMP.IsFullyParameterized(treatmentBMPModelingAttribute),
            WatershedName = treatmentBMP.Watershed?.WatershedName,
            Notes = treatmentBMP.Notes
        };
        return treatmentBmpDisplayDto;
    }

    public static TreatmentBMPUpsertDto AsUpsertDtoWithModelingAttributes(this TreatmentBMP treatmentBMP, vTreatmentBMPModelingAttribute treatmentBMPModelingAttribute)
    {
        var treatmentBMPUpsertDto = new TreatmentBMPUpsertDto()
        {
            TreatmentBMPID = treatmentBMP.TreatmentBMPID,
            TreatmentBMPName = treatmentBMP.TreatmentBMPName,
            TreatmentBMPTypeID = treatmentBMP.TreatmentBMPTypeID,
            TreatmentBMPTypeName = treatmentBMP.TreatmentBMPType.TreatmentBMPTypeName,
            TreatmentBMPModelingTypeID = treatmentBMP.TreatmentBMPType.TreatmentBMPModelingTypeID,
            WatershedName = treatmentBMP.Watershed?.WatershedName,
            StormwaterJurisdictionName = treatmentBMP.StormwaterJurisdiction.Organization.OrganizationName,
            Longitude = treatmentBMP.LocationPoint4326?.Coordinate.X,
            Latitude = treatmentBMP.LocationPoint4326?.Coordinate.Y,
            Notes = treatmentBMP.Notes,
            ModelingAttributes = treatmentBMP.CustomAttributes.Where(x => x.CustomAttributeType.CustomAttributeTypePurposeID == (int)CustomAttributeTypePurposeEnum.Modeling).Select(x => x.AsUpsertDto()).ToList(),
            AreAllModelingAttributesComplete = !treatmentBMP.TreatmentBMPType.MissingModelingAttributes(treatmentBMPModelingAttribute).Any(),
            IsFullyParameterized = treatmentBMP.IsFullyParameterized(treatmentBMPModelingAttribute)
        };

        return treatmentBMPUpsertDto;
    }

    public static bool IsFullyParameterized(this TreatmentBMP treatmentBMP, Delineation? delineation, vTreatmentBMPModelingAttribute treatmentBMPModelingAttribute)
    {
        // Planning BMPs don't need verified delineations
        // assumes the delineation passed in is the from the "upstreamest" BMP
        if (treatmentBMP.ProjectID == null && !(delineation?.IsVerified ?? false))
        {
            return false;
        }

        var treatmentBMPType = treatmentBMP.TreatmentBMPType;
        return !treatmentBMPType.MissingModelingAttributes(treatmentBMPModelingAttribute).Any();
    }

    public static bool IsFullyParameterized(this TreatmentBMP treatmentBMP, vTreatmentBMPModelingAttribute bmpModelingAttributes)
    {
        if (treatmentBMP.TreatmentBMPType.TreatmentBMPModelingTypeID == null)
        {
            return false;
        }

        if (treatmentBMP.Delineation == null)
        {
            return false;
        }

        return !treatmentBMP.TreatmentBMPType.MissingModelingAttributes(bmpModelingAttributes).Any();
    }

    public static void SetTreatmentBMPPointInPolygonDataByLocationPoint(this TreatmentBMP treatmentBMP,
        Geometry locationPoint, NeptuneDbContext dbContext)
    {
        treatmentBMP.WatershedID = dbContext.Watersheds.AsNoTracking()
            .FirstOrDefault(x => locationPoint.Intersects(x.WatershedGeometry))?.WatershedID;
        treatmentBMP.ModelBasinID = dbContext.ModelBasins.AsNoTracking()
            .FirstOrDefault(x => locationPoint.Intersects(x.ModelBasinGeometry))?.ModelBasinID;
        treatmentBMP.PrecipitationZoneID = dbContext.PrecipitationZones.AsNoTracking()
            .FirstOrDefault(x => locationPoint.Intersects(x.PrecipitationZoneGeometry))?.PrecipitationZoneID;
        treatmentBMP.RegionalSubbasinID = dbContext.RegionalSubbasins.AsNoTracking()
            .FirstOrDefault(x => locationPoint.Intersects(x.CatchmentGeometry))?.RegionalSubbasinID;
    }
}