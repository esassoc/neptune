using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptune.EFModels.Entities
{
    public partial class NeptuneDbContext
    {
        public NeptuneDbContext(string connectionString) : this(GetOptions(connectionString))
        {
        }

        /// <summary>
        /// Every DateTime read out of the database comes back as <see cref="DateTimeKind.Utc"/>.
        ///
        /// WHY THIS EXISTS. Every date this app stores is UTC -- `DateTime.UtcNow` at the write site,
        /// in a SQL `datetime` column, which carries no offset. So the value is right and its Kind is
        /// a lie: ADO.NET hands a `datetime` back as `Unspecified`, and .NET reads Unspecified as
        /// LOCAL.
        ///
        /// That lie was being acted on. DateTimeConverter serializes with `value.ToUniversalTime()`,
        /// which on an Unspecified value converts FROM the host's local time -- so an already-UTC
        /// reading of 19:00 went out as "2026-09-05T02:00:00Z" on a UTC-7 host. A different instant,
        /// stamped Z, with nothing to signal it was wrong. Production never showed it because the pods
        /// run UTC, where that conversion is a no-op; every developer machine showed it.
        ///
        /// Stamping the Kind on read makes `ToUniversalTime()` genuinely a no-op, which is the fix.
        /// It is also what any future move to DateTimeOffset rests on: a Utc-kind DateTime converts
        /// to an offset of +00:00 on any host, an Unspecified one takes the host's. See NPT-1127.
        ///
        /// The store direction is deliberately the identity: a Utc-kind DateTime writes its UTC
        /// wall-clock reading, which is exactly what the column is meant to hold.
        /// </summary>
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            // BOTH registrations, though the first covers the second: EF applies a converter
            // registered for DateTime to DateTime? as well, measured here by removing this line and
            // watching the nullable test still pass. Kept explicit because the nullable case is the
            // one worth being unambiguous about, not because it is required.
            configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeValueConverter>();
            configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeValueConverter>();
        }

        private static DbContextOptions<NeptuneDbContext> GetOptions(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(connectionString, x =>
            {
                x.CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds);
                x.UseNetTopologySuite();
            });
            return optionsBuilder.Options;
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
        }

        public virtual DbSet<RegionalSubbasinNetworkResult> RegionalSubbasinNetworkResults { get; set; }
    }

    /// <summary>
    /// Reads a `datetime` column back as UTC. See the comment on ConfigureConventions above for why
    /// the Kind matters at all; it is applied to nullable properties as well, EF unwrapping the null.
    /// </summary>
    internal class UtcDateTimeValueConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeValueConverter()
            : base(toStore => toStore, fromStore => DateTime.SpecifyKind(fromStore, DateTimeKind.Utc))
        {
        }
    }
}
