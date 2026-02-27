namespace Web.Features.Public.Authentication.ConfirmEmail;

public class ConfirmEmailRequest
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
}