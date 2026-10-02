using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;
using Neptune.Models.DataTransferObjects.Person;

namespace Neptune.EFModels.Entities;

// NPT-734: invite a person who has not signed in yet. The Person row is created with a null
// GlobalID and the chosen role and jurisdictions. On their first login People.UpdateClaims finds
// the row by email and stamps the Auth0 sub onto it, so the access is already in place.
public static class PersonInvites
{
    public const string EmailKey = "Email";
    public const string RoleKey = "RoleID";
    public const string OrganizationKey = "OrganizationID";
    public const string JurisdictionsKey = "StormwaterJurisdictionIDs";

    // Which roles an inviter may hand out. Admins mirror UserController.UpdateRole (no SitkaAdmin).
    // Jurisdiction Managers may only create Editors and Managers for their own jurisdiction.
    public static List<int> ListInvitableRoleIDs(PersonDto inviter)
    {
        return inviter.RoleID switch
        {
            (int)RoleEnum.SitkaAdmin => [(int)RoleEnum.SitkaAdmin, (int)RoleEnum.Admin, (int)RoleEnum.JurisdictionManager, (int)RoleEnum.JurisdictionEditor, (int)RoleEnum.Unassigned],
            (int)RoleEnum.Admin => [(int)RoleEnum.Admin, (int)RoleEnum.JurisdictionManager, (int)RoleEnum.JurisdictionEditor, (int)RoleEnum.Unassigned],
            (int)RoleEnum.JurisdictionManager => [(int)RoleEnum.JurisdictionManager, (int)RoleEnum.JurisdictionEditor],
            _ => [],
        };
    }

    // Accepts a bare address or the "Display Name <address>" form Outlook produces on copy.
    // The legacy MVC invite crashed on exactly that pasted form (NPT-734 stack trace).
    public static bool TryNormalizeEmail(string rawEmail, [NotNullWhen(true)] out string? normalizedEmail)
    {
        normalizedEmail = null;
        if (string.IsNullOrWhiteSpace(rawEmail))
        {
            return false;
        }

        // Exactly one '@': MailAddress quietly accepts "a@x.com, b@y.com" and keeps only the first, which would
        // invite the wrong number of people. Commas alone are fine, since Outlook copies "Doe, Jane <jane@x.com>".
        if (rawEmail.Count(c => c == '@') != 1)
        {
            return false;
        }

        if (!MailAddress.TryCreate(rawEmail.Trim(), out var mailAddress))
        {
            return false;
        }

        // MailAddress accepts dotless hosts like "jane@localhost"; a real invitee needs a routable domain.
        if (!mailAddress.Host.Contains('.'))
        {
            return false;
        }

        normalizedEmail = mailAddress.Address.Trim().ToLowerInvariant();
        return true;
    }

    public static async Task<List<ErrorMessage>> ValidateInviteAsync(NeptuneDbContext dbContext, PersonDto inviter, PersonInviteDto dto)
    {
        var errors = new List<ErrorMessage>();

        if (!TryNormalizeEmail(dto.Email, out var email))
        {
            errors.Add(new ErrorMessage(EmailKey, "Enter a single email address, like jane.doe@example.com."));
        }
        else
        {
            var existing = await dbContext.People.AsNoTracking()
                .Where(x => x.Email != null && x.Email.Trim().ToLower() == email)
                .Select(x => new { x.FirstName, x.LastName })
                .FirstOrDefaultAsync();
            if (existing != null)
            {
                var name = $"{existing.FirstName} {existing.LastName}".Trim();
                var who = string.IsNullOrEmpty(name) ? email : $"{name} ({email})";
                errors.Add(new ErrorMessage(EmailKey,
                    $"{who} already has an OC Stormwater Tools account. To give them access to a jurisdiction, edit that jurisdiction's assigned users instead."));
            }
        }

        var invitableRoleIDs = ListInvitableRoleIDs(inviter);
        if (dto.RoleID == null || !invitableRoleIDs.Contains(dto.RoleID.Value))
        {
            errors.Add(new ErrorMessage(RoleKey, "You are not allowed to invite someone with that role."));
        }

        if (dto.OrganizationID.HasValue && !await dbContext.Organizations.AnyAsync(x => x.OrganizationID == dto.OrganizationID.Value))
        {
            errors.Add(new ErrorMessage(OrganizationKey, "The selected organization does not exist."));
        }

        var jurisdictionIDs = (dto.StormwaterJurisdictionIDs ?? []).Distinct().ToList();
        var existingJurisdictionCount = await dbContext.StormwaterJurisdictions.CountAsync(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID));
        if (existingJurisdictionCount != jurisdictionIDs.Count)
        {
            errors.Add(new ErrorMessage(JurisdictionsKey, "One or more of the selected jurisdictions does not exist."));
        }
        else if (inviter.RoleID == (int)RoleEnum.JurisdictionManager)
        {
            if (jurisdictionIDs.Count != 1)
            {
                errors.Add(new ErrorMessage(JurisdictionsKey, "Choose exactly one of your jurisdictions for this person."));
            }
            else
            {
                var isAssigned = await dbContext.StormwaterJurisdictionPeople.AnyAsync(x => x.PersonID == inviter.PersonID && x.StormwaterJurisdictionID == jurisdictionIDs[0]);
                if (!isAssigned)
                {
                    errors.Add(new ErrorMessage(JurisdictionsKey, "You can only invite people to a jurisdiction you are assigned to."));
                }
            }
        }

        return errors;
    }

    // Call ValidateInviteAsync first; this assumes a valid, non-duplicate request.
    public static async Task<PersonDto?> InviteAsync(NeptuneDbContext dbContext, PersonInviteDto dto)
    {
        if (!TryNormalizeEmail(dto.Email, out var email))
        {
            throw new ArgumentException("Invite email is not a valid address; validate before inviting.", nameof(dto));
        }

        var person = new Person
        {
            Email = email,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            RoleID = dto.RoleID!.Value,
            OrganizationID = dto.OrganizationID ?? Organizations.OrganizationIDUnassigned,
            IsActive = true,
            ReceiveSupportEmails = false,
            ReceiveRSBRevisionRequestEmails = false,
            IsOCTAGrantReviewer = false,
            CreateDate = DateTime.UtcNow,
            GlobalID = null,
        };

        foreach (var stormwaterJurisdictionID in (dto.StormwaterJurisdictionIDs ?? []).Distinct())
        {
            person.StormwaterJurisdictionPeople.Add(new StormwaterJurisdictionPerson { StormwaterJurisdictionID = stormwaterJurisdictionID });
        }

        dbContext.People.Add(person);
        await dbContext.SaveChangesAsync();

        return await People.GetByIDAsDtoAsync(dbContext, person.PersonID);
    }

    public static async Task<List<string>> ListJurisdictionNamesAsync(NeptuneDbContext dbContext, IEnumerable<int> stormwaterJurisdictionIDs)
    {
        var ids = stormwaterJurisdictionIDs.ToList();
        return await dbContext.StormwaterJurisdictions.AsNoTracking()
            .Where(x => ids.Contains(x.StormwaterJurisdictionID))
            .Select(x => x.Organization.OrganizationName)
            .OrderBy(x => x)
            .ToListAsync();
    }
}
