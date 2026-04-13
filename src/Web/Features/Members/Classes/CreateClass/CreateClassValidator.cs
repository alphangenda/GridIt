using FluentValidation;

namespace Web.Features.Members.Classes.CreateClass;

public class CreateClassValidator : AbstractValidator<CreateClassRequest>
{
    public CreateClassValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
