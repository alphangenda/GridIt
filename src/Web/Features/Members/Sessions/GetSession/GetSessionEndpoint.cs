using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using IMapper = AutoMapper.IMapper;

namespace Web.Features.Members.Sessions.GetSession;

public class GetSessionEndpoint : Endpoint<GetSessionRequest, SessionDto>
{
    private readonly IMapper _mapper;
    private readonly ISessionRepository _sessionRepository;

    public GetSessionEndpoint(IMapper mapper, ISessionRepository sessionRepository)
    {
        _mapper = mapper;
        _sessionRepository = sessionRepository;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Get("sessions/{id}");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(GetSessionRequest req, CancellationToken ct)
    {
        var session = _sessionRepository.FindById(req.Id);
        await Send.OkAsync(_mapper.Map<SessionDto>(session), ct);
    }
}
