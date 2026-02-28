namespace Application.Services.Notifications.Models;

public class TwoFactorAuthenticationNotificationModel : NotificationModel
{
    public string Code { get; set; }
    
    public TwoFactorAuthenticationNotificationModel(string destination, string locale, string code) 
        : base(destination, locale)
    {
        Code = code;
    }

    public override string TemplateId()
    {
        if (Locale == "fr")
            return "d-c954f7e0b2614e5da6d9173e8d8c10ac";
        return "d-64fdc9a790224be7a0ae8ecea5a63372";
    }

    public override object TemplateData()
    {
        return new
        {
            Code
        };
    }
}