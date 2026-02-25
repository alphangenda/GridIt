using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Persistence.Extensions;
using IMapper = AutoMapper.IMapper;

namespace Web.Features.Members.Sessions.GetAllSessions;

public class GetAllSessionsEndpoint : EndpointWithoutRequest<List<SessionDto>>
{
    private readonly IMapper _mapper;
    private readonly ISessionRepository _sessionRepository;

    public GetAllSessionsEndpoint(IMapper mapper, ISessionRepository sessionRepository)
    {
        _mapper = mapper;
        _sessionRepository = sessionRepository;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Get("sessions");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userEmail = HttpContext.GetUserEmail() ?? "";
        var sessions = _sessionRepository.GetAllByUser(userEmail);
        await Send.OkAsync(_mapper.Map<List<SessionDto>>(sessions), ct);
    }
}
