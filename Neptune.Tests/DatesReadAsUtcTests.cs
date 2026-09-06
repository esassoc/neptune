using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1127 phase 1: a DateTime read out of the database knows it is UTC.
    ///
    /// WHAT WAS WRONG. Every date this app stores is UTC -- `DateTime.UtcNow` at the write site, into a
    /// SQL `datetime` column, which carries no offset. ADO.NET returns such a column as
    /// `Kind=Unspecified`, and .NET reads Unspecified as LOCAL. DateTimeConverter then serialized with
    /// `value.ToUniversalTime()`, which on an Unspecified value converts FROM the host's local time --
    /// so an already-UTC reading of 19:00 went out as "2026-09-05T02:00:00Z" on a UTC-7 host. A
    /// different instant, stamped Z, with nothing to signal it was wrong.
    ///
    /// Production never showed it: the pods run UTC (no TZ in the charts, and the aspnet base image
    /// defaults to it), where that conversion is a no-op. Every developer machine showed it. That
    /// asymmetry is why it survived -- it is invisible in the environment that matters and misleading
    /// in the one people work in.
    ///
    /// WHY THESE TESTS ASSERT ON Kind RATHER THAN ON THE SERIALIZED STRING. The serialized string is
    /// only wrong when the host is not UTC, so a test asserting on it would pass in CI while the bug
    /// was live. Kind is the property the convention actually guarantees, it is the same on every
    /// host, and it is what makes `ToUniversalTime()` a no-op by definition. Assert the cause, not the
    /// symptom that happens to be visible from here.
    ///
    /// READ-ONLY, and inside a transaction that is rolled back regardless, following the pattern in
    /// CustomAttributesMarkDirtyTests. These read rows that are already there rather than making any.
    /// </summary>
    [TestClass]
    public class DatesReadAsUtcTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;

        private static NeptuneDbContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(
                "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;",
                x =>
                {
                    x.CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds);
                    x.UseNetTopologySuite();
                });
            return new NeptuneDbContext(optionsBuilder.Options);
        }

        [TestInitialize]
        public void Setup()
        {
            _dbContext = GetDbContext();
            _transaction = _dbContext.Database.BeginTransaction();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _dbContext.Dispose();
        }

        /// <summary>The non-nullable case. Without the convention this comes back Unspecified.</summary>
        [TestMethod]
        public void ANonNullableDateComesBackAsUtc()
        {
            var createDate = _dbContext.FileResources.AsNoTracking()
                .OrderBy(x => x.FileResourceID)
                .Select(x => x.CreateDate)
                .First();

            Assert.AreEqual(DateTimeKind.Utc, createDate.Kind,
                "a datetime read out of the database should know it is UTC, which is what stops ToUniversalTime shifting it");
        }

        /// <summary>
        /// The nullable case. Worth its own test because most of the date fields in this app are
        /// nullable -- 53 of the 102 on the DTOs -- so "it works for DateTime" is not the interesting
        /// half.
        ///
        /// Not because the nullable registration is required: EF applies a converter registered for
        /// DateTime to DateTime? on its own, measured by removing that line and watching this pass
        /// anyway. An earlier version of this comment claimed the opposite.
        /// </summary>
        [TestMethod]
        public void ANullableDateComesBackAsUtc()
        {
            var lastUpdate = _dbContext.TrashGeneratingUnits.AsNoTracking()
                .Where(x => x.LastUpdateDate != null)
                .OrderBy(x => x.TrashGeneratingUnitID)
                .Select(x => x.LastUpdateDate)
                .First();

            Assert.IsNotNull(lastUpdate, "this test needs a row with a value; none was found");
            Assert.AreEqual(DateTimeKind.Utc, lastUpdate!.Value.Kind,
                "a nullable datetime read out of the database should know it is UTC too");
        }

        /// <summary>
        /// The convention LABELS, it does not convert. This is the assertion that would catch someone
        /// "fixing" it with ToUniversalTime on the read side, which would move every date in the
        /// database by the host's offset -- the same bug one layer down.
        /// </summary>
        [TestMethod]
        public void TheConventionLabelsTheValueWithoutMovingIt()
        {
            var fileResourceID = _dbContext.FileResources.AsNoTracking()
                .OrderBy(x => x.FileResourceID).Select(x => x.FileResourceID).First();

            var throughEf = _dbContext.FileResources.AsNoTracking()
                .Where(x => x.FileResourceID == fileResourceID)
                .Select(x => x.CreateDate)
                .Single();

            // The same column read as text, so the comparison is against what SQL Server actually holds
            // rather than against another EF materialization carrying the same convention.
            var asStored = _dbContext.Database
                .SqlQuery<string>($"SELECT CONVERT(varchar(30), CreateDate, 121) AS Value FROM dbo.FileResource WHERE FileResourceID = {fileResourceID}")
                .Single();

            Assert.AreEqual(asStored, throughEf.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                "the wall-clock reading changed on the way out of the database; the convention should only be stamping the Kind");
        }
    }
}
