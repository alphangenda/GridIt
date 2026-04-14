using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Web.Features.Admins.Programs.GetAllPrograms;

public class GetAllProgramsEndpoint : EndpointWithoutRequest<List<ProgramDto>>
{
    private readonly GarneauTemplateDbContext _dbContext;

    public GetAllProgramsEndpoint(GarneauTemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Get("programs");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var programs = await _dbContext.CoursePrograms
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProgramDto
            {
                Id = p.Id,
                Name = p.Name
            })
            .ToListAsync(ct);

        await Send.OkAsync(programs, ct);
    }
}
