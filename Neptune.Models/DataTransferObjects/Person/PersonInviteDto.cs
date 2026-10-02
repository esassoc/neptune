using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Neptune.Models.DataTransferObjects.Person
{
    public class PersonInviteDto
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        // Not [EmailAddress]: that attribute only checks for a single '@', so it accepts
        // "Jane Doe <jane@example.com>" pasted from Outlook (the NPT-734 legacy crash input).
        // PersonInvites.TryNormalizeEmail parses and normalizes the address instead.
        [Required]
        [MaxLength(255)]
        public string Email { get; set; }

        [Required]
        public int? RoleID { get; set; }

        public int? OrganizationID { get; set; }

        public List<int> StormwaterJurisdictionIDs { get; set; } = new();
    }
}
