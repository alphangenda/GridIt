using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Extensions;

namespace Web.Features.Grids.GetGrids;

public class GetGridsEndpoint : EndpointWithoutRequest<List<GridDto>>
{
    private readonly GarneauTemplateDbContext _context;

    public GetGridsEndpoint(GarneauTemplateDbContext context)
    {
        _context = context;
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
        var grids = ownResults.Concat(publicResults)
            .GroupBy(r => r.ExamId)
            .Select(g => g.First())
            .OrderByDescending(r => r.Created)
            .Select(r => new GridDto(
                r.ExamId,
                r.ClassId,
                r.ExamName,
                r.ClassName,
                r.SessionName,
                r.IsPublic,
                r.CreatorEmail == userEmail,
                r.CreatorEmail,
                r.Created.ToDateTimeUtc()
            )).ToList();

        await Send.OkAsync(grids, ct);
    }
}
