namespace Neptune.Models.DataTransferObjects.Person
{
    // NPT-734: the invited Person plus whether the invitation email actually went out, so the UI
    // doesn't say "Invitation sent" when SendGrid rejected it.
    public class PersonInviteResultDto
    {
        public PersonDto Person { get; set; }
        public bool InvitationEmailSent { get; set; }
    }
}
