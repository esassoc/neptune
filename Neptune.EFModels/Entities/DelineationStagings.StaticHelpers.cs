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
            .Intersect(centralizedDelineations, StringComparer.InvariantCultureIgnoreCase)
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

        // Match staged names with a server-side subquery (not an in-memory list) so very large uploads don't
        // exceed SQL Server's ~2,100-parameter cap on an IN (@p0..@pN) clause. The staging rows were just
        // persisted above, so this subquery is exactly this user's current upload.
        var stagedNamesQuery = dbContext.DelineationStagings
            .Where(s => s.UploadedByPersonID == currentPerson.PersonID)
            .Select(s => s.TreatmentBMPName);
        var bmpsWithUpstreamSet = dbContext.TreatmentBMPs.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stagingJurisdictionID
                        && stagedNamesQuery.Contains(x.TreatmentBMPName)
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

        var stormwaterJurisdictionID = stagings.Select(x => x.StormwaterJurisdictionID).Distinct().Single();

        // Match staged BMP names with a server-side subquery rather than an in-memory list: a list translates
        // to IN (@p0..@pN) — one parameter per staged name — which exceeds SQL Server's ~2,100-parameter cap
        // on very large uploads. The subquery matches in the DB (case-insensitive via collation) with no
        // per-name parameters.
        var stagedNamesQuery = dbContext.DelineationStagings
            .Where(s => s.UploadedByPersonID == currentPerson.PersonID)
            .Select(s => s.TreatmentBMPName);

        // Scope to the staging's jurisdiction AND distributed type so a same-named BMP in another jurisdiction
        // (or a centralized delineation in this one) doesn't get deleted by a Distributed-only upload.
        var delineationsToDelete = dbContext.Delineations.AsNoTracking()
            .Include(x => x.TreatmentBMP)
            .Where(x => x.TreatmentBMP.StormwaterJurisdictionID == stormwaterJurisdictionID
                        && x.DelineationTypeID == (int)DelineationTypeEnum.Distributed
                        && stagedNamesQuery.Contains(x.TreatmentBMP.TreatmentBMPName))
            .Select(x => x.DelineationID)
            .ToList();
        // ExecuteDelete bypasses the change tracker, so detach any tracked instances from earlier in this
        // DbContext's lifetime first — otherwise they'd still occupy the 1:1 nav slot and collide with the new
        // Delineations added below. Then delete the whole set with a fixed number of statements rather than
        // ~12 per delineation (a 1,000+ delineation re-upload would otherwise be ~13k sequential roundtrips).
        var idsToDelete = delineationsToDelete.ToHashSet();
        foreach (var tracked in dbContext.ChangeTracker.Entries<Delineation>()
                     .Where(e => idsToDelete.Contains(e.Entity.DelineationID)).ToList())
        {
            tracked.State = EntityState.Detached;
        }
        await Delineation.DeleteFullForMany(dbContext, delineationsToDelete);

        var bmpsToUpdate = dbContext.TreatmentBMPs.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID && stagedNamesQuery.Contains(x.TreatmentBMPName))
            .Select(x => new { x.TreatmentBMPID, x.TreatmentBMPName })
            .ToList();

        foreach (var bmp in bmpsToUpdate)
        {
            // Mirror the SQL-side comparison that populated bmpsToUpdate: SQL Server collation matches names
            // case-insensitively and ignores trailing whitespace, so a plain ordinal == here misses case/whitespace-only
            // differences and throws "Sequence contains no matching element".
            var staging = stagings.First(z =>
                string.Equals(z.TreatmentBMPName?.Trim(), bmp.TreatmentBMPName?.Trim(),
                    StringComparison.InvariantCultureIgnoreCase));
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
