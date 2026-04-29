using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Persistence;

namespace Web.Features.Admins.Programs.ProgramSkills;

public class RemoveProgramSkillEndpoint : EndpointWithoutRequest
{
    private readonly GarneauTemplateDbContext _dbContext;
    private readonly IClassSkillRepository _classSkillRepository;
    private readonly IConfiguration _configuration;

    public RemoveProgramSkillEndpoint(
        GarneauTemplateDbContext dbContext,
        IClassSkillRepository classSkillRepository,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _classSkillRepository = classSkillRepository;
        _configuration = configuration;
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

        // Trouver tous les cours liés à ce programme
        var classIds = await _dbContext.Classes
            .AsNoTracking()
            .Where(c => c.ProgramId == programId)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (classIds.Count > 0)
        {
            // Supprimer la compétence des exam_skills pour les examens de ces cours
            var examIds = await _dbContext.Exams
                .AsNoTracking()
                .Where(e => classIds.Contains(e.ClassId))
                .Select(e => e.Id)
                .ToListAsync(ct);

            if (examIds.Count > 0)
            {
                var examSkillsToRemove = await _dbContext.ExamSkills
                    .Where(es => examIds.Contains(es.ExamId) && es.SkillId == skillId)
                    .ToListAsync(ct);

                _dbContext.ExamSkills.RemoveRange(examSkillsToRemove);
            }

            // Supprimer la compétence des class_skills pour ces cours
            foreach (var classId in classIds)
            {
                await _classSkillRepository.DeleteClassSkill(classId, skillId);
            }
        }

        _dbContext.ProgramSkills.Remove(link);
        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
