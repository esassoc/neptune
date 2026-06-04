namespace Neptune.ExternalAPI.Controllers;

/// <summary>
/// Blob filenames produced by <c>Neptune.Jobs</c> Hangfire jobs and served as-is by this API.
/// Duplicated here so <c>Neptune.ExternalAPI</c> doesn't need a project reference to
/// <c>Neptune.Jobs</c> — these strings are part of the storage contract, not the job logic.
/// </summary>
internal static class BlobFileNames
{
    public const string LandUseStatistics = "LandUseStatistics.json";
    public const string ModelResults = "ModelResults.json";
    public const string BaselineModelResults = "BaselineModelResults.json";
}
