namespace Application.Services.Notifications.Models;

public class RegisterConfirmationNotificationModel : NotificationModel
{
    public string Link { get; set; }

    public RegisterConfirmationNotificationModel(string destination, string locale, string link)
        : base(destination, locale)
    {
        Link = link;
    }

    public override string TemplateId()
    {
        if (Locale == "fr")
            return "d-c41370e579eb471c8088ce1a00d4e6b4";
        return "d-d97cfe7e5a0e45c595f6097aa454f2d5";
    }

    public override object TemplateData()
    {
        return new
        {
            lien_activation = Link
        };
    }
}