using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Web.Features.Members.Sessions.DeleteSession;

public class DeleteSessionEndpoint : Endpoint<DeleteSessionRequest, EmptyResponse>
{
    private readonly ISessionRepository _sessionRepository;

    public DeleteSessionEndpoint(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Delete("sessions/{id}");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(DeleteSessionRequest request, CancellationToken ct)
    {
        await _sessionRepository.DeleteSessionWithId(request.Id);
        await Send.NoContentAsync(ct);
    }
}
