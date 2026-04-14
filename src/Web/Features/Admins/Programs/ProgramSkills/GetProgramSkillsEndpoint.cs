using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Web.Dtos;

namespace Web.Features.Admins.Programs.ProgramSkills;

public class GetProgramSkillsEndpoint : EndpointWithoutRequest<List<SkillDto>>
{
    private readonly GarneauTemplateDbContext _dbContext;

    public GetProgramSkillsEndpoint(GarneauTemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Get("programs/{programId:guid}/skills");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var programId = Route<Guid>("programId");

        var skills = await _dbContext.ProgramSkills
            .AsNoTracking()
            .Where(ps => ps.ProgramId == programId)
            .Join(_dbContext.Skills,
                ps => ps.SkillId,
                s => s.Id,
                (ps, s) => new SkillDto
                {
                    Id = s.Id,
                    Label = s.Label
                })
            .OrderBy(s => s.Label)
            .ToListAsync(ct);

        await Send.OkAsync(skills, ct);
    }
}
