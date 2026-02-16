using FastEndpoints;
using FluentValidation;

namespace Web.Features.Members.Sessions.CreateSession;

public class CreateSessionValidator : Validator<CreateSessionRequest>
{
    public CreateSessionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
