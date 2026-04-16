using Domain.Entities.Classes;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Web.Dtos;

namespace Web.Features.Skills.CreateSkill;

public class CreateSkillEndpoint : Endpoint<CreateSkillRequest, SkillDto>
{
    private readonly GarneauTemplateDbContext _dbContext;

    public CreateSkillEndpoint(GarneauTemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Post("skills");
        Roles(Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CreateSkillRequest req, CancellationToken ct)
    {
        var label = req.Label?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(label))
        {
            AddError(nameof(CreateSkillRequest.Label), "Skill label is required.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var existing = await _dbContext.Skills
            .FirstOrDefaultAsync(s => s.Label.ToLower() == label.ToLower(), ct);

        if (existing is not null)
        {
            await Send.OkAsync(new SkillDto { Id = existing.Id, Label = existing.Label }, ct);
            return;
        }

        var skill = new Domain.Entities.Classes.Skill();
        skill.SetId(Guid.NewGuid());
        skill.SetLabel(label);

        _dbContext.Skills.Add(skill);
        await _dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new SkillDto { Id = skill.Id, Label = skill.Label }, ct);
    }
}
