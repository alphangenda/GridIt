using Domain.Entities.Classes;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Web.Features.Admins.Programs.ProgramSkills;

public class SaveProgramSkillsEndpoint : Endpoint<SaveProgramSkillsRequest>
{
    private readonly GarneauTemplateDbContext _dbContext;

    public SaveProgramSkillsEndpoint(GarneauTemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Post("programs/{programId:guid}/skills");
        Roles(Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(SaveProgramSkillsRequest req, CancellationToken ct)
    {
        var programId = Route<Guid>("programId");
        var uniqueSkillIds = req.SkillIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var existing = await _dbContext.ProgramSkills
            .Where(ps => ps.ProgramId == programId)
            .ToListAsync(ct);

        _dbContext.ProgramSkills.RemoveRange(existing);

        foreach (var skillId in uniqueSkillIds)
        {
            var programSkill = new ProgramSkill();
            programSkill.SetProgramId(programId);
            programSkill.SetSkillId(skillId);
            _dbContext.ProgramSkills.Add(programSkill);
        }

        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
