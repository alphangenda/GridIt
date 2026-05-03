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
            return "d-8769cf9127e441f1b6e43fa2bc578b6d";
        return "d-ad8652c741784647aaf496571548b48f";
    }

    public override object TemplateData()
    {
        return new
        {
            Code
        };
    }
}