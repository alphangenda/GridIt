using FastEndpoints;
using FluentValidation;

namespace Web.Features.Public.Authentication.Register;

public class RegisterValidator : Validator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotNull()
            .NotEmpty()
            .EmailAddress()
            .WithErrorCode("InvalidEmail")
            .WithMessage("Email should not be empty.");

        RuleFor(x => x.Password)
            .NotNull()
            .NotEmpty()
            .WithErrorCode("InvalidPassword")
            .WithMessage("Password should not be empty.");

        RuleFor(x => x.ConfirmEmailRelativeUrl)
            .NotNull()
            .NotEmpty()
            .WithErrorCode("InvalidConfirmEmailRelativeUrl")
            .WithMessage("Confirm email relative path should not be empty.");
    }
}