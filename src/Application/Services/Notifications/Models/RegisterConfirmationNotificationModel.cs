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
            return "d-3bb310eb4473484a9ce437f8d1cbe12c";
        return "d-e927b1c9689a4bc688dcc6cba985f512";
    }

    public override object TemplateData()
    {
        return new
        {
            lien_activation = Link
        };
    }
}