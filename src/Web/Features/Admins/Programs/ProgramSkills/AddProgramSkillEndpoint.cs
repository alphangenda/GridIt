using Domain.Entities.Classes;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Web.Features.Admins.Programs.ProgramSkills;

public class AddProgramSkillEndpoint : EndpointWithoutRequest
{
    private readonly GarneauTemplateDbContext _dbContext;

    public AddProgramSkillEndpoint(GarneauTemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Post("programs/{programId:guid}/skills/{skillId:guid}");
        Roles(Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var programId = Route<Guid>("programId");
        var skillId = Route<Guid>("skillId");

        var programExists = await _dbContext.CoursePrograms.AnyAsync(p => p.Id == programId, ct);
        var skillExists = await _dbContext.Skills.AnyAsync(s => s.Id == skillId, ct);
        if (!programExists || !skillExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var alreadyLinked = await _dbContext.ProgramSkills
            .AnyAsync(ps => ps.ProgramId == programId && ps.SkillId == skillId, ct);

        if (alreadyLinked)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        var link = new ProgramSkill();
        link.SetProgramId(programId);
        link.SetSkillId(skillId);
        _dbContext.ProgramSkills.Add(link);

        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
