using System;

namespace Neptune.Tests;

/// <summary>
/// The one place database-backed tests get their connection string, so the whole suite can be aimed at
/// a database other than a SQL Server on the developer's own machine.
///
/// Resolution order:
///  1. NEPTUNE_TEST_CONNECTION_STRING — an explicit override, used as given.
///  2. DB_SERVER + DB_PASSWORD (+ DB_NAME) — the variables the devcontainer already sets for its SQL
///     Server container, so `make api-test` works there with no extra setup.
///  3. localhost with Windows authentication — a SQL Server installed on the developer's machine.
/// </summary>
public static class TestDatabase
{
    private const string LocalhostDefault =
        "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;";

    public static string ConnectionString { get; } = Resolve();

    private static string Resolve()
    {
        var overrideConnectionString = Environment.GetEnvironmentVariable("NEPTUNE_TEST_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(overrideConnectionString))
        {
            return overrideConnectionString;
        }

        var server = Environment.GetEnvironmentVariable("DB_SERVER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (!string.IsNullOrWhiteSpace(server) && !string.IsNullOrWhiteSpace(password))
        {
            var database = Environment.GetEnvironmentVariable("DB_NAME");
            if (string.IsNullOrWhiteSpace(database))
            {
                database = "NeptuneDB";
            }

            return $"Data Source={server};Initial Catalog={database};User ID=sa;Password={password};Encrypt=False;TrustServerCertificate=True;";
        }

        return LocalhostDefault;
    }
}
