using FastEndpoints;
using FluentValidation;

namespace Web.Features.Members.Sessions.UpdateSession;

public class UpdateSessionValidator : Validator<UpdateSessionRequest>
{
    public UpdateSessionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
