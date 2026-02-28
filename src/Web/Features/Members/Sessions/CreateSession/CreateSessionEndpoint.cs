using Domain.Entities.Classes;
using Domain.Entities.Sessions;
using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;
using IMapper = AutoMapper.IMapper;

namespace Web.Features.Members.Sessions.CreateSession;

public class CreateSessionEndpoint : Endpoint<CreateSessionRequest, SessionDto>
{
    private readonly IMapper _mapper;
    private readonly ISessionRepository _sessionRepository;
    private readonly IClassRepository _classRepository;
    private readonly GarneauTemplateDbContext _context;

    public CreateSessionEndpoint(IMapper mapper, ISessionRepository sessionRepository, IClassRepository classRepository, GarneauTemplateDbContext context)
    {
        _mapper = mapper;
        _sessionRepository = sessionRepository;
        _classRepository = classRepository;
        _context = context;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Post("sessions");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CreateSessionRequest req, CancellationToken ct)
    {
        var session = new Session();
        session.SetName(req.Name.Trim());
        session.SetId(Guid.NewGuid());

        // Add classes to session - load with tracking to avoid duplicate key errors
        if (req.ClassIds != null && req.ClassIds.Any())
        {
            var classes = await _context.Classes
                .Where(c => req.ClassIds.Contains(c.Id))
                .ToListAsync(ct);
            
            foreach (var classEntity in classes)
            {
                session.AddClass(classEntity);
            }
        }

        await _sessionRepository.CreateSession(session);
        
        // Reload session with classes to return complete data
        var createdSession = _sessionRepository.FindById(session.Id);
        await Send.OkAsync(_mapper.Map<SessionDto>(createdSession), ct);
    }
}
