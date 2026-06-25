using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

public static class WebServiceAccessLogs
{
    // Called from WebServiceAccessLogMiddleware (Neptune.ExternalAPI) after each
    // authenticated request resolves. Inserts the audit row + denormalizes the
    // timestamp onto Person.LastWebServiceAccessDate in one round trip so a single
    // ExecuteUpdate keeps the "who's idle" query fast (no scan of the log table).
    public static async Task CreateAsync(NeptuneDbContext dbContext, int personID, string endpoint, string httpMethod, int responseStatusCode)
    {
        var now = DateTime.UtcNow;

        var log = new WebServiceAccessLog
        {
            PersonID = personID,
            Endpoint = endpoint,
            HttpMethod = httpMethod,
            RequestedDate = now,
            ResponseStatusCode = responseStatusCode,
        };
        dbContext.WebServiceAccessLogs.Add(log);
        await dbContext.SaveChangesAsync();

        await dbContext.People
            .Where(x => x.PersonID == personID)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.LastWebServiceAccessDate, now));
    }
}
