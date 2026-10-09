using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Controllers;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.Common.Email;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using Neptune.Models.DataTransferObjects.Person;
using Neptune.Models.Helpers;
using SendGrid;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-734: email normalization for invites. The legacy MVC invite crashed (NullReferenceException)
    /// when an inviter pasted "Display Name &lt;address&gt;" from Outlook. No database needed.
    /// </summary>
    [TestClass]
    public class InviteEmailNormalizationTests
    {
        [TestMethod]
        public void OutlookDisplayNameForm_ExtractsAddress()
        {
            Assert.IsTrue(PersonInvites.TryNormalizeEmail("Stephanie Castle Zinn <SCastleZinn@fuscoe.com>", out var email));
            Assert.AreEqual("scastlezinn@fuscoe.com", email);
        }

        [TestMethod]
        public void OutlookLastCommaFirstForm_ExtractsAddress()
        {
            Assert.IsTrue(PersonInvites.TryNormalizeEmail("Doe, Jane <Jane.Doe@example.com>", out var email));
            Assert.AreEqual("jane.doe@example.com", email);
        }

        [TestMethod]
        public void BareAddress_IsTrimmedAndLowercased()
        {
            Assert.IsTrue(PersonInvites.TryNormalizeEmail("  Jane.Doe@Example.COM ", out var email));
            Assert.AreEqual("jane.doe@example.com", email);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("not an email")]
        [DataRow("jane@")]
        [DataRow("jane@localhost")]
        [DataRow("jane@example.com, bob@example.com")]
        [DataRow("jane@example.com; bob@example.com")]
        public void Garbage_IsRejected(string raw)
        {
            Assert.IsFalse(PersonInvites.TryNormalizeEmail(raw, out _), $"Expected '{raw}' to be rejected.");
        }
    }

    /// <summary>
    /// NPT-734: SendGrid reports rejections in its response rather than throwing, so SitkaSmtpClientService.Send
    /// must return false for them. Uses a real SendGridClient over a stub HTTP handler; nothing leaves the machine.
    /// </summary>
    [TestClass]
    public class SitkaSmtpClientServiceSendResultTests
    {
        private sealed class StubHandler(HttpStatusCode statusCode) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent("{\"errors\":[{\"message\":\"stub\"}]}") });
        }

        private static SitkaSmtpClientService ServiceReturning(HttpStatusCode statusCode)
        {
            var sendGridClient = new SendGridClient(new HttpClient(new StubHandler(statusCode)), "SG.test-key-not-real");
            var configuration = Options.Create(new SendGridConfiguration { SendGridApiKey = "SG.test-key-not-real" });
            return new SitkaSmtpClientService(sendGridClient, configuration, NullLogger<SitkaSmtpClientService>.Instance);
        }

        private static MailMessage Message()
        {
            var message = new MailMessage { Subject = "test", Body = "<p>test</p>", IsBodyHtml = true, From = new MailAddress("donotreply@example.com") };
            message.To.Add("someone@example.com");
            return message;
        }

        [TestMethod]
        public async Task Accepted_ReturnsTrue()
        {
            Assert.IsTrue(await ServiceReturning(HttpStatusCode.Accepted).Send(Message()));
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.TooManyRequests)]
        [DataRow(HttpStatusCode.InternalServerError)]
        public async Task Rejected_ReturnsFalse(HttpStatusCode statusCode)
        {
            Assert.IsFalse(await ServiceReturning(statusCode).Send(Message()));
        }
    }

    /// <summary>
    /// NPT-734: Admins and Jurisdiction Managers can invite. JMs are scoped to their own jurisdiction in
    /// PersonInvites.ValidateInviteAsync, since [JurisdictionManageFeature] is role-only.
    /// </summary>
    [TestClass]
    public class InviteUserAuthorizationTests
    {
        private static MethodInfo GetInviteMethod()
        {
            var method = typeof(UserController).GetMethod(nameof(UserController.Invite), BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(method, "Expected Invite on UserController.");
            return method;
        }

        [TestMethod]
        public void Invite_RequiresJurisdictionManageFeature()
        {
            Assert.IsTrue(GetInviteMethod().GetCustomAttributes(typeof(JurisdictionManageFeature), true).Any(),
                "UserController.Invite must carry [JurisdictionManageFeature].");
        }

        [TestMethod]
        public void Invite_IsNotAnonymous()
        {
            var method = GetInviteMethod();
            Assert.IsFalse(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any(), "Invite must not be [AllowAnonymous].");
            Assert.IsFalse(method.GetCustomAttributes(typeof(OptionalAuthAttribute), true).Any(), "Invite must not be [OptionalAuth].");
        }

        [TestMethod]
        public void InvitableRoles_MatchTheRules()
        {
            CollectionAssert.AreEquivalent(
                new[] { (int)RoleEnum.JurisdictionManager, (int)RoleEnum.JurisdictionEditor },
                PersonInvites.ListInvitableRoleIDs(new PersonDto { RoleID = (int)RoleEnum.JurisdictionManager }));
            CollectionAssert.DoesNotContain(PersonInvites.ListInvitableRoleIDs(new PersonDto { RoleID = (int)RoleEnum.Admin }), (int)RoleEnum.SitkaAdmin);
            CollectionAssert.Contains(PersonInvites.ListInvitableRoleIDs(new PersonDto { RoleID = (int)RoleEnum.SitkaAdmin }), (int)RoleEnum.SitkaAdmin);
            Assert.AreEqual(0, PersonInvites.ListInvitableRoleIDs(new PersonDto { RoleID = (int)RoleEnum.JurisdictionEditor }).Count);
            Assert.AreEqual(0, PersonInvites.ListInvitableRoleIDs(new PersonDto { RoleID = (int)RoleEnum.Unassigned }).Count);
        }
    }

    /// <summary>
    /// NPT-734: the serializable begin/commit path in PersonInvites.InviteAsync. Kept out of InviteUserTests on
    /// purpose: that class's setup holds an uncommitted Person insert, which this test's serializable read would
    /// block on. Commits for real, then deletes its row.
    /// </summary>
    [TestClass]
    public class InviteUserTransactionTests
    {
        private static NeptuneDbContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(
                "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;",
                x =>
                {
                    x.CommandTimeout((int)TimeSpan.FromSeconds(30).TotalSeconds);
                    x.UseNetTopologySuite();
                });
            return new NeptuneDbContext(optionsBuilder.Options);
        }

        [TestMethod]
        public async Task InviteAsync_OwnsASerializableTransaction_WhenTheCallerHasNone()
        {
            await using var dbContext = GetDbContext();
            var dto = new PersonInviteDto
            {
                FirstName = "Invited",
                LastName = "Person",
                Email = $"npt734-{Guid.NewGuid():N}@example.com",
                RoleID = (int)RoleEnum.Unassigned,
            };
            int? personID = null;
            try
            {
                var result = await PersonInvites.InviteAsync(dbContext, dto);
                personID = result.Person?.PersonID;

                Assert.IsNotNull(result.Person);
                Assert.IsNull(dbContext.Database.CurrentTransaction, "InviteAsync should commit and dispose its own transaction.");
                await using var verifyContext = GetDbContext();
                Assert.IsTrue(await verifyContext.People.AnyAsync(x => x.PersonID == result.Person.PersonID), "The invite should be committed.");

                var again = await PersonInvites.InviteAsync(dbContext, dto);
                Assert.IsNull(again.Person, "A second invite for the same address must be refused.");
            }
            finally
            {
                if (personID.HasValue)
                {
                    await using var cleanupContext = GetDbContext();
                    await cleanupContext.People.Where(x => x.PersonID == personID.Value).ExecuteDeleteAsync();
                }
            }
        }
    }

    /// <summary>
    /// NPT-734: invite validation and creation against the local NeptuneDB, rolled back after each test.
    /// Fixtures: a Jurisdiction Manager assigned to one jurisdiction, and a second jurisdiction they are not on.
    /// </summary>
    [TestClass]
    public class InviteUserTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;
        private PersonDto _manager = null!;
        private readonly PersonDto _admin = new() { PersonID = -1, RoleID = (int)RoleEnum.Admin };
        private int _managersJurisdictionID;
        private int _otherJurisdictionID;

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

        private static string UniqueEmail() => $"npt734-{Guid.NewGuid():N}@example.com";

        [TestInitialize]
        public void Setup()
        {
            _dbContext = GetDbContext();
            _transaction = _dbContext.Database.BeginTransaction();

            var jurisdictionIDs = _dbContext.StormwaterJurisdictions.AsNoTracking().OrderBy(x => x.StormwaterJurisdictionID)
                .Select(x => x.StormwaterJurisdictionID).Take(2).ToList();
            Assert.AreEqual(2, jurisdictionIDs.Count, "Tests require at least two StormwaterJurisdiction rows in the local DB.");
            _managersJurisdictionID = jurisdictionIDs[0];
            _otherJurisdictionID = jurisdictionIDs[1];

            var manager = new Person
            {
                FirstName = "Test",
                LastName = "Manager",
                Email = UniqueEmail(),
                RoleID = (int)RoleEnum.JurisdictionManager,
                OrganizationID = Organizations.OrganizationIDUnassigned,
                IsActive = true,
                CreateDate = DateTime.UtcNow,
            };
            manager.StormwaterJurisdictionPeople.Add(new StormwaterJurisdictionPerson { StormwaterJurisdictionID = _managersJurisdictionID });
            _dbContext.People.Add(manager);
            _dbContext.SaveChanges();
            _manager = new PersonDto { PersonID = manager.PersonID, RoleID = manager.RoleID, Email = manager.Email };
        }

        [TestCleanup]
        public void Teardown()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _dbContext?.Dispose();
        }

        private static PersonInviteDto Dto(int roleID, params int[] jurisdictionIDs) => new()
        {
            FirstName = "Invited",
            LastName = "Person",
            Email = UniqueEmail(),
            RoleID = roleID,
            StormwaterJurisdictionIDs = jurisdictionIDs.ToList(),
        };

        private static void AssertHasError(List<ErrorMessage> errors, string key)
        {
            Assert.IsTrue(errors.Any(x => x.Type == key), $"Expected a '{key}' error; got: {string.Join(" | ", errors.Select(x => $"{x.Type}: {x.Message}"))}");
        }

        [TestMethod]
        public async Task AdminInvite_CreatesPersonWithRoleAndJurisdictionsAndNoGlobalID()
        {
            var dto = Dto((int)RoleEnum.JurisdictionEditor, _managersJurisdictionID, _otherJurisdictionID);
            Assert.AreEqual(0, (await PersonInvites.ValidateInviteAsync(_dbContext, _admin, dto)).Count);

            var invited = (await PersonInvites.InviteAsync(_dbContext, dto)).Person;

            Assert.IsNotNull(invited);
            var person = _dbContext.People.AsNoTracking().Include(x => x.StormwaterJurisdictionPeople).Single(x => x.PersonID == invited.PersonID);
            Assert.IsNull(person.GlobalID);
            Assert.AreEqual((int)RoleEnum.JurisdictionEditor, person.RoleID);
            Assert.AreEqual(Organizations.OrganizationIDUnassigned, person.OrganizationID);
            Assert.IsTrue(person.IsActive);
            CollectionAssert.AreEquivalent(new[] { _managersJurisdictionID, _otherJurisdictionID },
                person.StormwaterJurisdictionPeople.Select(x => x.StormwaterJurisdictionID).ToList());
        }

        [TestMethod]
        public async Task PastedOutlookAddress_IsStoredNormalized()
        {
            var address = UniqueEmail();
            var dto = Dto((int)RoleEnum.JurisdictionEditor, _managersJurisdictionID);
            dto.Email = $"Invited Person <{address.ToUpperInvariant()}>";
            Assert.AreEqual(0, (await PersonInvites.ValidateInviteAsync(_dbContext, _manager, dto)).Count);

            var invited = (await PersonInvites.InviteAsync(_dbContext, dto)).Person;

            Assert.IsNotNull(invited);
            Assert.AreEqual(address, invited.Email);
        }

        [TestMethod]
        public async Task InviteAsync_RechecksDuplicates_WhenARaceSlipsPastValidation()
        {
            // Both requests validate before either inserts, as two simultaneous invites would.
            var first = Dto((int)RoleEnum.JurisdictionEditor, _managersJurisdictionID);
            var second = Dto((int)RoleEnum.JurisdictionEditor, _managersJurisdictionID);
            second.Email = first.Email.ToUpperInvariant();
            Assert.AreEqual(0, (await PersonInvites.ValidateInviteAsync(_dbContext, _admin, first)).Count);
            Assert.AreEqual(0, (await PersonInvites.ValidateInviteAsync(_dbContext, _admin, second)).Count);

            var firstResult = await PersonInvites.InviteAsync(_dbContext, first);
            var secondResult = await PersonInvites.InviteAsync(_dbContext, second);

            Assert.IsNotNull(firstResult.Person);
            Assert.IsNull(secondResult.Person);
            Assert.IsNotNull(secondResult.DuplicateEmailMessage);
            Assert.AreEqual(1, _dbContext.People.AsNoTracking().Count(x => x.Email == first.Email), "No duplicate Person row.");
        }

        [TestMethod]
        public async Task RepeatedJurisdictionID_IsListedOnceInTheEmail_AndAssignedOnce()
        {
            // Copilot (PR #689) asked whether [id, id] lists the jurisdiction twice in the email.
            // The name lookup selects jurisdiction rows, so a repeated ID yields one name.
            var names = await PersonInvites.ListJurisdictionNamesAsync(_dbContext, [_managersJurisdictionID, _managersJurisdictionID]);
            Assert.AreEqual(1, names.Count);

            var dto = Dto((int)RoleEnum.JurisdictionEditor, _managersJurisdictionID, _managersJurisdictionID);
            Assert.AreEqual(0, (await PersonInvites.ValidateInviteAsync(_dbContext, _manager, dto)).Count);
            var invited = (await PersonInvites.InviteAsync(_dbContext, dto)).Person;
            Assert.IsNotNull(invited);
            Assert.AreEqual(1, _dbContext.StormwaterJurisdictionPeople.AsNoTracking().Count(x => x.PersonID == invited.PersonID));
        }

        [TestMethod]
        public async Task GarbageEmail_IsAValidationErrorNotAnException()
        {
            var dto = Dto((int)RoleEnum.JurisdictionEditor, _managersJurisdictionID);
            dto.Email = "not an email";
            AssertHasError(await PersonInvites.ValidateInviteAsync(_dbContext, _manager, dto), PersonInvites.EmailKey);
        }

        [TestMethod]
        public async Task DuplicateEmail_IsRejected_IgnoringCaseAndWhitespace()
        {
            var dto = Dto((int)RoleEnum.JurisdictionEditor, _managersJurisdictionID);
            dto.Email = $"  {_manager.Email.ToUpperInvariant()} ";
            AssertHasError(await PersonInvites.ValidateInviteAsync(_dbContext, _admin, dto), PersonInvites.EmailKey);
        }

        [TestMethod]
        [DataRow((int)RoleEnum.Admin)]
        [DataRow((int)RoleEnum.SitkaAdmin)]
        [DataRow((int)RoleEnum.Unassigned)]
        public async Task Manager_CannotAssignRolesOutsideJurisdictionRoles(int roleID)
        {
            AssertHasError(await PersonInvites.ValidateInviteAsync(_dbContext, _manager, Dto(roleID, _managersJurisdictionID)), PersonInvites.RoleKey);
        }

        [TestMethod]
        public async Task Manager_CanInviteEditorOrManagerToOwnJurisdiction()
        {
            Assert.AreEqual(0, (await PersonInvites.ValidateInviteAsync(_dbContext, _manager, Dto((int)RoleEnum.JurisdictionEditor, _managersJurisdictionID))).Count);
            Assert.AreEqual(0, (await PersonInvites.ValidateInviteAsync(_dbContext, _manager, Dto((int)RoleEnum.JurisdictionManager, _managersJurisdictionID))).Count);
        }

        [TestMethod]
        public async Task Manager_CannotInviteToAnotherJurisdiction()
        {
            AssertHasError(await PersonInvites.ValidateInviteAsync(_dbContext, _manager, Dto((int)RoleEnum.JurisdictionEditor, _otherJurisdictionID)), PersonInvites.JurisdictionsKey);
        }

        [TestMethod]
        public async Task Manager_MustChooseExactlyOneJurisdiction()
        {
            AssertHasError(await PersonInvites.ValidateInviteAsync(_dbContext, _manager, Dto((int)RoleEnum.JurisdictionEditor)), PersonInvites.JurisdictionsKey);
            AssertHasError(await PersonInvites.ValidateInviteAsync(_dbContext, _manager, Dto((int)RoleEnum.JurisdictionEditor, _managersJurisdictionID, _otherJurisdictionID)), PersonInvites.JurisdictionsKey);
        }

        [TestMethod]
        public async Task Admin_CannotAssignSitkaAdmin_ButSitkaAdminCan()
        {
            AssertHasError(await PersonInvites.ValidateInviteAsync(_dbContext, _admin, Dto((int)RoleEnum.SitkaAdmin)), PersonInvites.RoleKey);
            var sitkaAdmin = new PersonDto { PersonID = -1, RoleID = (int)RoleEnum.SitkaAdmin };
            Assert.AreEqual(0, (await PersonInvites.ValidateInviteAsync(_dbContext, sitkaAdmin, Dto((int)RoleEnum.SitkaAdmin))).Count);
        }

        [TestMethod]
        public async Task UnknownJurisdiction_IsRejected()
        {
            AssertHasError(await PersonInvites.ValidateInviteAsync(_dbContext, _admin, Dto((int)RoleEnum.JurisdictionEditor, int.MaxValue)), PersonInvites.JurisdictionsKey);
        }

        [TestMethod]
        public async Task FirstLogin_LinksInvitedPersonByEmail_KeepingRoleAndJurisdiction()
        {
            var invited = (await PersonInvites.InviteAsync(_dbContext, Dto((int)RoleEnum.JurisdictionEditor, _managersJurisdictionID))).Person;
            Assert.IsNotNull(invited);
            var sub = $"auth0|npt734-{Guid.NewGuid():N}";

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimsConstants.Sub, sub),
                new Claim(ClaimsConstants.Emails, invited.Email),
                new Claim(ClaimsConstants.GivenName, "Invited"),
                new Claim(ClaimsConstants.FamilyName, "Person"),
            }, "test"));

            var linked = await People.UpdateClaims(_dbContext, principal);

            Assert.IsNotNull(linked);
            Assert.AreEqual(invited.PersonID, linked.PersonID, "First login must reuse the invited Person, not create a new one.");
            var person = _dbContext.People.AsNoTracking().Include(x => x.StormwaterJurisdictionPeople).Single(x => x.PersonID == invited.PersonID);
            Assert.AreEqual(sub, person.GlobalID);
            Assert.AreEqual((int)RoleEnum.JurisdictionEditor, person.RoleID);
            CollectionAssert.AreEquivalent(new[] { _managersJurisdictionID }, person.StormwaterJurisdictionPeople.Select(x => x.StormwaterJurisdictionID).ToList());
            Assert.AreEqual(1, _dbContext.People.AsNoTracking().Count(x => x.Email == invited.Email), "No duplicate Person row.");
        }
    }
}
