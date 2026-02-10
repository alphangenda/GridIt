using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Web.Features.Members.Classes.DeleteExam;

public class DeleteExamEndpoint : Endpoint<DeleteExamRequest, EmptyResponse>
{
    private readonly IExamRepository _examRepository;

    public DeleteExamEndpoint(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Delete("exams/{id}");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(DeleteExamRequest request, CancellationToken ct)
    {
        await _examRepository.DeleteExamWithId(request.Id);
        await Send.NoContentAsync(ct);
    }
}
