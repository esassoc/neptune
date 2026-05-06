using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Common.GeoSpatial;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class DelineationStagings
{
    public static async Task<List<string>> ProcessDeserializedStagingAsync(
        NeptuneDbContext dbContext,
        byte[] geoJsonBytes,
        Person currentPerson)
    {
        var errors = new List<string>();

        var delineationStagings = await GeoJsonSerializer.DeserializeFromFeatureCollectionWithCCWCheck<DelineationStaging>(
            geoJsonBytes, GeoJsonSerializer.DefaultSerializerOptions, Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID);

        var validDelineationStagings = delineationStagings
            .Where(x => x.Geometry is { IsValid: true, Area: > 0 })
            .ToList();

        if (validDelineationStagings.Count == 0)
        {
            errors.Add("No valid delineation polygons were found in the uploaded file.");
            return errors;
        }

        // Scope the centralized-conflict check to the staging's own jurisdiction so a same-named BMP in another
        // jurisdiction doesn't cause a false-positive block.
        var stagingJurisdictionID = validDelineationStagings.First().StormwaterJurisdictionID;
        var centralizedDelineations = dbContext.Delineations.AsNoTracking()
            .Where(x => x.DelineationTypeID == (int)DelineationTypeEnum.Centralized
                        && x.TreatmentBMP.StormwaterJurisdictionID == stagingJurisdictionID)
            .Select(x => x.TreatmentBMP.TreatmentBMPName)
            .ToList();

        var centralizedConflicts = validDelineationStagings.Select(x => x.TreatmentBMPName)
            .Intersect(centralizedDelineations)
            .ToList();
        if (centralizedConflicts.Count > 0)
        {
            errors.Add(
                $"This file contains the following Treatment BMPs that have centralized delineations: {string.Join(", ", centralizedConflicts)}. The file is invalid and cannot be uploaded.");
            return errors;
        }

        await dbContext.DelineationStagings
            .Where(x => x.UploadedByPersonID == currentPerson.PersonID)
            .ExecuteDeleteAsync();
        dbContext.DelineationStagings.AddRange(validDelineationStagings);
        await dbContext.SaveChangesAsync();

        var stagedNames = validDelineationStagings.Select(x => x.TreatmentBMPName).ToList();
        var bmpsWithUpstreamSet = dbContext.TreatmentBMPs.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stagingJurisdictionID
                        && stagedNames.Contains(x.TreatmentBMPName)
                        && x.UpstreamBMPID != null)
            .Select(x => x.TreatmentBMPName)
            .ToList();
        foreach (var name in bmpsWithUpstreamSet)
        {
            errors.Add($"Treatment BMP \"{name}\" has an Upstream BMP and cannot accept delineations. Remove this Treatment BMP from your file or remove the Upstream BMP from the Treatment BMP and try again.");
        }

        return errors;
    }

    public static DelineationGdbUploadValidationDto BuildReportForCurrentUser(NeptuneDbContext dbContext, Person currentPerson)
    {
        var dto = new DelineationGdbUploadValidationDto();
        var stagings = dbContext.DelineationStagings.AsNoTracking()
            .Include(x => x.StormwaterJurisdiction)
            .Where(x => x.UploadedByPersonID == currentPerson.PersonID)
            .ToList();

        if (stagings.Count == 0)
        {
            dto.Errors.Add("No staged delineations were found for the current user. Please upload a file before approving.");
            return dto;
        }

        var jurisdictions = stagings.Select(x => x.StormwaterJurisdictionID).Distinct().ToList();
        if (jurisdictions.Count > 1)
        {
            dto.Errors.Add($"Multiple Stormwater Jurisdictions were staged for user {currentPerson.PersonID}. Please re-upload with a single jurisdiction.");
            return dto;
        }

        var stormwaterJurisdictionID = jurisdictions.Single();
        dto.StormwaterJurisdictionID = stormwaterJurisdictionID;

        var existingDistributedDelineations = dbContext.Delineations.AsNoTracking()
            .Include(x => x.TreatmentBMP)
            .Where(x => x.TreatmentBMP.StormwaterJurisdictionID == stormwaterJurisdictionID
                        && x.DelineationTypeID == (int)DelineationTypeEnum.Distributed)
            .ToList();

        var bmpNamesInJurisdiction = TreatmentBMPs.GetNonPlanningModuleBMPs(dbContext)
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID)
            .Select(x => x.TreatmentBMPName)
            .ToList();

        var candidateNames = stagings.Select(x => x.TreatmentBMPName).Distinct().ToList();
        var numberOfDelineations = stagings.Count;
        if (candidateNames.Count != numberOfDelineations)
        {
            dto.Errors.Add("The Treatment BMP Name must be unique for each feature in the upload.");
        }

        var unmatchedNames = candidateNames
            .Except(bmpNamesInJurisdiction, StringComparer.InvariantCultureIgnoreCase)
            .ToList();
        if (unmatchedNames.Count > 0)
        {
            dto.Errors.Add($"{unmatchedNames.Count} Delineations were found that do not match a Treatment BMP Name in the selected Jurisdiction: {string.Join(", ", unmatchedNames)}");
        }

        var badGeometryNames = stagings.Where(x => x.Geometry.IsValid == false).Select(x => x.TreatmentBMPName).ToList();
        if (badGeometryNames.Count > 0)
        {
            dto.Errors.Add("The following Delineations have bad geometries: " + string.Join(", ", badGeometryNames));
        }

        var numberToBeUpdated = existingDistributedDelineations
            .Count(x => candidateNames.Contains(x.TreatmentBMP.TreatmentBMPName, StringComparer.InvariantCultureIgnoreCase));

        dto.NumberOfDelineations = numberOfDelineations;
        dto.NumberOfDelineationsToBeUpdated = numberToBeUpdated;
        dto.NumberOfDelineationsToBeCreated = numberOfDelineations - numberToBeUpdated - unmatchedNames.Count;
        return dto;
    }

    public static async Task<int> ApproveAsync(NeptuneDbContext dbContext, Person currentPerson)
    {
        var stagings = dbContext.DelineationStagings.AsNoTracking()
            .Where(x => x.UploadedByPersonID == currentPerson.PersonID)
            .ToList();
        Check.Assert(stagings.Count > 0, "No staged delineations were found for the current user.");

        var stagedNames = stagings.Select(x => x.TreatmentBMPName).ToList();
        var stormwaterJurisdictionID = stagings.Select(x => x.StormwaterJurisdictionID).Distinct().Single();

        var delineationsToDelete = dbContext.Delineations.AsNoTracking()
            .Include(x => x.TreatmentBMP)
            .Where(x => stagedNames.Contains(x.TreatmentBMP.TreatmentBMPName))
            .Select(x => x.DelineationID)
            .ToList();
        foreach (var delineationID in delineationsToDelete)
        {
            // ExecuteDelete bypasses the change tracker, so any tracked instance from earlier in this DbContext's
            // lifetime would still occupy the 1:1 nav slot and collide with the new Delineation we add below.
            var tracked = dbContext.ChangeTracker.Entries<Delineation>()
                .FirstOrDefault(e => e.Entity.DelineationID == delineationID);
            if (tracked != null)
            {
                tracked.State = EntityState.Detached;
            }
            await Delineation.DeleteFull(dbContext, delineationID);
        }

        var bmpsToUpdate = dbContext.TreatmentBMPs.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID && stagedNames.Contains(x.TreatmentBMPName))
            .Select(x => new { x.TreatmentBMPID, x.TreatmentBMPName })
            .ToList();

        foreach (var bmp in bmpsToUpdate)
        {
            var staging = stagings.Single(z => z.TreatmentBMPName == bmp.TreatmentBMPName);
            dbContext.Delineations.Add(new Delineation
            {
                HasDiscrepancies = false,
                IsVerified = staging.DelineationStatus?.Trim().ToLower() == "verified",
                DelineationTypeID = (int)DelineationTypeEnum.Distributed,
                TreatmentBMPID = bmp.TreatmentBMPID,
                DateLastModified = DateTime.UtcNow,
                VerifiedByPersonID = currentPerson.PersonID,
                DateLastVerified = DateTime.UtcNow,
                DelineationGeometry4326 = staging.Geometry.ProjectTo4326(),
                DelineationGeometry = staging.Geometry,
            });
        }

        await dbContext.SaveChangesAsync();
        await dbContext.DelineationStagings
            .Where(x => x.UploadedByPersonID == currentPerson.PersonID)
            .ExecuteDeleteAsync();

        return bmpsToUpdate.Count;
    }

    public static async Task DiscardForUserAsync(NeptuneDbContext dbContext, Person currentPerson)
    {
        await dbContext.DelineationStagings
            .Where(x => x.UploadedByPersonID == currentPerson.PersonID)
            .ExecuteDeleteAsync();
    }
}
