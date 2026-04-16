using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Web.Features.Admins.Programs.ProgramSkills;

public class RemoveProgramSkillEndpoint : EndpointWithoutRequest
{
    private readonly GarneauTemplateDbContext _dbContext;

    public RemoveProgramSkillEndpoint(GarneauTemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Delete("programs/{programId:guid}/skills/{skillId:guid}");
        Roles(Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var programId = Route<Guid>("programId");
        var skillId = Route<Guid>("skillId");

        var link = await _dbContext.ProgramSkills
            .FirstOrDefaultAsync(ps => ps.ProgramId == programId && ps.SkillId == skillId, ct);

        if (link is null)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        _dbContext.ProgramSkills.Remove(link);
        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
