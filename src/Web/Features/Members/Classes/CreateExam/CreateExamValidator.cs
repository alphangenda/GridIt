using FastEndpoints;
using FluentValidation;

namespace Web.Features.Members.Classes.CreateExam;

public class CreateExamValidator : Validator<CreateExamRequest>
{
    public CreateExamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
