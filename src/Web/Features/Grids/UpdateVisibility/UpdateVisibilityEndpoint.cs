using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Extensions;

namespace Web.Features.Grids.UpdateVisibility;

public class UpdateVisibilityEndpoint : Endpoint<UpdateVisibilityRequest>
{
    private readonly GarneauTemplateDbContext _context;

    public UpdateVisibilityEndpoint(GarneauTemplateDbContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Patch("grids/{ExamId}/visibility");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(UpdateVisibilityRequest req, CancellationToken ct)
    {
        var userEmail = HttpContext.GetUserEmail() ?? "";

        // Verify the exam belongs to the current user (via session ownership)
        var exam = await (
            from session in _context.Sessions
            join sc in _context.SessionClasses on session.Id equals sc.SessionId
            join cls in _context.Classes on sc.ClassId equals cls.Id
            join e in _context.Exams on cls.Id equals e.ClassId
            where e.Id == req.ExamId && session.CreatedBy == userEmail
            select e
        ).FirstOrDefaultAsync(ct);

        if (exam is null)
        {
            HttpContext.Response.StatusCode = 404;
            return;
        }

        exam.SetIsPublic(req.IsPublic);
        await _context.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
