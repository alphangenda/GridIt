using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Web.Features.Members.Classes.DeleteClass;

public class DeleteClassEndpoint : Endpoint<DeleteClassRequest, EmptyResponse>
{
    private readonly IClassRepository _classRepository;

    public DeleteClassEndpoint(IClassRepository classRepository)
    {
        _classRepository = classRepository;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Delete("classes/{id}");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(DeleteClassRequest request, CancellationToken ct)
    {
        await _classRepository.DeleteClassWithId(request.Id);
        await Send.NoContentAsync(ct);
    }
}
