using Application.Exceptions.Sessions;
using Domain.Entities.Classes;
using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;
using IMapper = AutoMapper.IMapper;

namespace Web.Features.Members.Sessions.UpdateSession;

public class UpdateSessionEndpoint : Endpoint<UpdateSessionRequest, SessionDto>
{
    private readonly IMapper _mapper;
    private readonly ISessionRepository _sessionRepository;
    private readonly IClassRepository _classRepository;
    private readonly GarneauTemplateDbContext _context;

    public UpdateSessionEndpoint(IMapper mapper, ISessionRepository sessionRepository, IClassRepository classRepository, GarneauTemplateDbContext context)
    {
        _mapper = mapper;
        _sessionRepository = sessionRepository;
        _classRepository = classRepository;
        _context = context;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Put("sessions/{id}");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(UpdateSessionRequest req, CancellationToken ct)
    {
        // Load session with tracking to update it
        var session = await _context.Sessions
            .Include(s => s.Classes)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        
        if (session == null)
            throw new SessionNotFoundException($"Could not find session with id {req.Id}.");

        session.SetName(req.Name.Trim());
        session.ClearClasses();

        // Add new classes to session - load with tracking
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

        await _sessionRepository.UpdateSession(session);
        
        // Reload to get updated data
        var updatedSession = _sessionRepository.FindById(req.Id);
        await Send.OkAsync(_mapper.Map<SessionDto>(updatedSession), ct);
    }
}
