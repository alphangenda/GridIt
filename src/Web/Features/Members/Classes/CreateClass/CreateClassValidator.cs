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

        RuleForEach(x => x.Students).ChildRules(student =>
        {
            student.RuleFor(s => s.Number).NotEmpty().MaximumLength(50);
            student.RuleFor(s => s.FirstName).NotEmpty().MaximumLength(200);
            student.RuleFor(s => s.LastName).NotEmpty().MaximumLength(200);
        });
    }
}
