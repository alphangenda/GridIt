using Web.Features.Common;

namespace Web.Features.Public.Authentication.Register;

public class RegisterRequest : ISanitizable
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmEmailRelativeUrl { get; set; } = null!;

    public void Sanitize()
    {
        Email = Email.Trim();
        ConfirmEmailRelativeUrl = ConfirmEmailRelativeUrl.Trim();
    }
}