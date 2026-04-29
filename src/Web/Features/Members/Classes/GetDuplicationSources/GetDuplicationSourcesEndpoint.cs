using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Extensions;

namespace Web.Features.Members.Classes.GetDuplicationSources;

public class GetDuplicationSourcesEndpoint : EndpointWithoutRequest<List<DuplicationSourceDto>>
{
    private readonly GarneauTemplateDbContext _context;

    public GetDuplicationSourcesEndpoint(GarneauTemplateDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Get("classes/duplication-sources");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userEmail = HttpContext.GetUserEmail() ?? "";

        // Get all classes accessible via sessions (own + others with public exams)
        var classesWithSession = await (
            from session in _context.Sessions
            join sc in _context.SessionClasses on session.Id equals sc.SessionId
            join cls in _context.Classes on sc.ClassId equals cls.Id
            where session.CreatedBy == userEmail
               || _context.Exams.Any(e => e.ClassId == cls.Id && e.IsPublic && session.CreatedBy != userEmail)
            select new
            {
                ClassId = cls.Id,
                ClassName = cls.Name,
                SessionName = session.Name,
                CreatorEmail = session.CreatedBy ?? "",
            }
        ).ToListAsync(ct);

        // Deduplicate by ClassId (a class could appear in multiple sessions)
        var uniqueClasses = classesWithSession
            .GroupBy(c => c.ClassId)
            .Select(g => g.First())
            .ToList();

        var classIds = uniqueClasses.Select(c => c.ClassId).ToList();

        // Load skills for these classes
        var classSkills = await (
            from cs in _context.Set<Domain.Entities.Classes.ExamSkill>()
            where false
            select cs
        ).ToListAsync(ct); // placeholder, we'll use raw query below

        // Load skills via class_skills join table (not an EF entity)
        var skillsByClass = new Dictionary<Guid, List<DuplicationSkillDto>>();
        foreach (var classId in classIds)
        {
            var skills = await _context.Database.SqlQueryRaw<DuplicationSkillDto>(
                "SELECT s.id AS \"Id\", s.label AS \"Label\" FROM class_skills cs INNER JOIN skills s ON s.id = cs.skill_id WHERE cs.class_id = {0} ORDER BY s.label",
                classId
            ).ToListAsync(ct);
            skillsByClass[classId] = skills;
        }

        // Load exams for these classes
        var exams = await _context.Exams
            .AsNoTracking()
            .Where(e => classIds.Contains(e.ClassId))
            .Select(e => new { e.Id, e.ClassId, e.Name, e.IsPublic })
            .ToListAsync(ct);

        var result = uniqueClasses.Select(c =>
        {
            var isOwner = c.CreatorEmail == userEmail;
            var classExams = exams
                .Where(e => e.ClassId == c.ClassId)
                .Where(e => isOwner || e.IsPublic) // non-owners only see public exams
                .Select(e => new DuplicationExamDto(e.Id, e.Name))
                .ToList();

            skillsByClass.TryGetValue(c.ClassId, out var skills);

            return new DuplicationSourceDto(
                c.ClassId,
                c.ClassName,
                c.SessionName,
                c.CreatorEmail,
                isOwner,
                skills ?? new List<DuplicationSkillDto>(),
                classExams
            );
        })
        .Where(c => c.Skills.Count > 0 || c.Exams.Count > 0) // only show classes with content to copy
        .OrderByDescending(c => c.IsOwner)
        .ThenBy(c => c.SessionName)
        .ThenBy(c => c.ClassName)
        .ToList();

        await Send.OkAsync(result, ct);
    }
}
