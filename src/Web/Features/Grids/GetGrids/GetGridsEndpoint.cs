using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Persistence;
using Persistence.Extensions;

namespace Web.Features.Grids.GetGrids;

public class GetGridsEndpoint : EndpointWithoutRequest<List<GridDto>>
{
    private readonly GarneauTemplateDbContext _context;
    private readonly IConfiguration _config;

    public GetGridsEndpoint(GarneauTemplateDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Get("grids");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userEmail = HttpContext.GetUserEmail() ?? "";

        // 1) Own grids (via sessions the user created)
        var ownResults = await (
            from session in _context.Sessions
            join sc in _context.SessionClasses on session.Id equals sc.SessionId
            join cls in _context.Classes on sc.ClassId equals cls.Id
            join exam in _context.Exams on cls.Id equals exam.ClassId
            where session.CreatedBy == userEmail
            select new
            {
                ExamId = exam.Id,
                ClassId = cls.Id,
                ExamName = exam.Name,
                ClassName = cls.Name,
                SessionName = session.Name,
                exam.IsPublic,
                CreatorEmail = session.CreatedBy ?? "",
                Created = exam.Created,
            }
        ).ToListAsync(ct);

        // 2) Public grids from other professors
        var publicResults = await (
            from session in _context.Sessions
            join sc in _context.SessionClasses on session.Id equals sc.SessionId
            join cls in _context.Classes on sc.ClassId equals cls.Id
            join exam in _context.Exams on cls.Id equals exam.ClassId
            where session.CreatedBy != userEmail && exam.IsPublic
            select new
            {
                ExamId = exam.Id,
                ClassId = cls.Id,
                ExamName = exam.Name,
                ClassName = cls.Name,
                SessionName = session.Name,
                exam.IsPublic,
                CreatorEmail = session.CreatedBy ?? "",
                Created = exam.Created,
            }
        ).ToListAsync(ct);

        // Combine, deduplicate by ExamId, order by most recent
        var combined = ownResults.Concat(publicResults)
            .GroupBy(r => r.ExamId)
            .Select(g => g.First())
            .OrderByDescending(r => r.Created)
            .ToList();

        // Load group names for all exams from exam_groups table
        var examGroupNames = new Dictionary<Guid, List<string>>();
        var cs = _config.GetConnectionString("DefaultConnection");
        using (var conn = new NpgsqlConnection(cs))
        {
            conn.Open();
            var examIds = combined.Select(r => r.ExamId).ToList();
            if (examIds.Count > 0)
            {
                var paramNames = examIds.Select((_, i) => $"@eid{i}").ToList();
                var cmd = new NpgsqlCommand(
                    $"SELECT exam_id, name FROM exam_groups WHERE exam_id IN ({string.Join(",", paramNames)}) ORDER BY name",
                    conn);
                for (var i = 0; i < examIds.Count; i++)
                    cmd.Parameters.AddWithValue($"@eid{i}", examIds[i]);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var eid = reader.GetGuid(0);
                    var name = reader.GetString(1);
                    if (!examGroupNames.ContainsKey(eid))
                        examGroupNames[eid] = new List<string>();
                    examGroupNames[eid].Add(name);
                }
            }
        }

        var grids = combined.Select(r => new GridDto(
                r.ExamId,
                r.ClassId,
                r.ExamName,
                r.ClassName,
                r.SessionName,
                r.IsPublic,
                r.CreatorEmail == userEmail,
                r.CreatorEmail,
                r.Created.ToDateTimeUtc(),
                examGroupNames.TryGetValue(r.ExamId, out var names) ? names : new List<string>()
            )).ToList();

        await Send.OkAsync(grids, ct);
    }
}
