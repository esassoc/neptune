using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// Covers the NPT-1078 generate/lookup helpers for <c>Person.WebServiceAccessToken</c>. The
    /// column went nullable in this story; auto-population at Person creation was removed and
    /// users now opt in via the SPA Web Services tab. The Neptune.ExternalAPI auth handlers
    /// (header-based and legacy route-segment) both delegate token validation to
    /// <see cref="People.GetByWebServiceAccessTokenAsync"/>, so the lookup behaviour here is
    /// the source of truth for both auth schemes.
    /// </summary>
    [TestClass]
    public class PeopleWebServiceTokenTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;
        private int _personID;

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

            // Pick any existing person; tests below null their token explicitly so seed state
            // doesn't matter.
            _personID = _dbContext.People.AsNoTracking().OrderBy(x => x.PersonID).Select(x => x.PersonID).FirstOrDefault();
            Assert.IsTrue(_personID > 0, "Tests require at least one Person row in the local DB.");

            var person = _dbContext.People.Single(x => x.PersonID == _personID);
            person.WebServiceAccessToken = null;
            _dbContext.SaveChanges();
        }

        [TestCleanup]
        public void Teardown()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _dbContext?.Dispose();
        }

        [TestMethod]
        public async Task GenerateAndPersist_SetsNewGuidAndReturnsIt()
        {
            var token = await People.GenerateAndPersistWebServiceAccessTokenAsync(_dbContext, _personID);

            Assert.AreNotEqual(Guid.Empty, token);
            var reloaded = _dbContext.People.AsNoTracking().Single(x => x.PersonID == _personID);
            Assert.AreEqual(token, reloaded.WebServiceAccessToken);
        }

        [TestMethod]
        public async Task GenerateAndPersist_RotatesExistingToken()
        {
            var first = await People.GenerateAndPersistWebServiceAccessTokenAsync(_dbContext, _personID);
            var second = await People.GenerateAndPersistWebServiceAccessTokenAsync(_dbContext, _personID);

            Assert.AreNotEqual(first, second);
            var reloaded = _dbContext.People.AsNoTracking().Single(x => x.PersonID == _personID);
            Assert.AreEqual(second, reloaded.WebServiceAccessToken);
        }

        [TestMethod]
        public async Task GetByWebServiceAccessTokenAsync_ReturnsPersonForValidToken()
        {
            var token = await People.GenerateAndPersistWebServiceAccessTokenAsync(_dbContext, _personID);

            var found = await People.GetByWebServiceAccessTokenAsync(_dbContext, token);

            Assert.IsNotNull(found);
            Assert.AreEqual(_personID, found!.PersonID);
        }

        [TestMethod]
        public async Task GetByWebServiceAccessTokenAsync_ReturnsNullForUnknownToken()
        {
            var found = await People.GetByWebServiceAccessTokenAsync(_dbContext, Guid.NewGuid());

            Assert.IsNull(found);
        }

        [TestMethod]
        public async Task GetByWebServiceAccessTokenAsync_ReturnsNullWhenColumnIsNull()
        {
            // Sanity: the nullable column shouldn't ever match a Guid lookup (the EF comparison
            // sees NULL ≠ any value).
            var found = await People.GetByWebServiceAccessTokenAsync(_dbContext, Guid.Empty);

            Assert.IsNull(found);
        }
    }
}
