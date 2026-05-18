namespace Neptune.Models.DataTransferObjects.Person;

public class PersonNotificationDto
{
    public int NotificationID { get; set; }
    public DateTime NotificationDate { get; set; }
    public int NotificationTypeID { get; set; }
    public string NotificationTypeDisplayName { get; set; }
}
