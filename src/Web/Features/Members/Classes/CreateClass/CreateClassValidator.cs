using FastEndpoints;
using FluentValidation;

namespace Web.Features.Members.Classes.CreateClass;

public class CreateClassValidator : Validator<CreateClassRequest>
{
    public CreateClassValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
