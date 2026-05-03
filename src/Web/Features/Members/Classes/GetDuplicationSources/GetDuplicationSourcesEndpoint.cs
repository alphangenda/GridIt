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

        var classesWithSession = await (
            from session in _context.Sessions
            join sc in _context.SessionClasses on session.Id equals sc.SessionId
            join cls in _context.Classes on sc.ClassId equals cls.Id
            join prog in _context.CoursePrograms on cls.ProgramId equals prog.Id into progJoin
            from prog in progJoin.DefaultIfEmpty()
            where session.CreatedBy == userEmail
               || _context.Exams.Any(e => e.ClassId == cls.Id && e.IsPublic && session.CreatedBy != userEmail)
            select new
            {
                ClassId = cls.Id,
                ClassName = cls.Name,
                SessionName = session.Name,
                CreatorEmail = session.CreatedBy ?? "",
                ProgramName = prog != null ? prog.Name : null,
            }
        ).ToListAsync(ct);

        var uniqueClasses = classesWithSession
            .GroupBy(c => c.ClassId)
            .Select(g => g.First())
            .ToList();

        var classIds = uniqueClasses.Select(c => c.ClassId).ToList();

        var skillsByClass = new Dictionary<Guid, List<DuplicationSkillDto>>();
        foreach (var classId in classIds)
        {
            var skills = await _context.Database.SqlQueryRaw<DuplicationSkillDto>(
                "SELECT s.id AS \"Id\", s.label AS \"Label\" FROM class_skills cs INNER JOIN skills s ON s.id = cs.skill_id WHERE cs.class_id = {0} ORDER BY s.label",
                classId
            ).ToListAsync(ct);
            skillsByClass[classId] = skills;
        }

        var result = uniqueClasses.Select(c =>
        {
            skillsByClass.TryGetValue(c.ClassId, out var skills);

            return new DuplicationSourceDto(
                c.ClassId,
                c.ClassName,
                c.SessionName,
                c.CreatorEmail,
                c.CreatorEmail == userEmail,
                c.ProgramName,
                skills ?? new List<DuplicationSkillDto>()
            );
        })
        .OrderByDescending(c => c.IsOwner)
        .ThenBy(c => c.SessionName)
        .ThenBy(c => c.ClassName)
        .ToList();

        await Send.OkAsync(result, ct);
    }
}
