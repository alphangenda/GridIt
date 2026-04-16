using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Web.Features.Admins.Programs.DeleteProgram;

public class DeleteProgramEndpoint : EndpointWithoutRequest
{
    private readonly GarneauTemplateDbContext _dbContext;

    public DeleteProgramEndpoint(GarneauTemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Delete("programs/{programId:guid}");
        Roles(Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var programId = Route<Guid>("programId");
        var program = await _dbContext.CoursePrograms.FirstOrDefaultAsync(x => x.Id == programId, ct);
        if (program is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var linkedClasses = await _dbContext.Classes.Where(x => x.ProgramId == programId).ToListAsync(ct);
        foreach (var classEntity in linkedClasses)
            classEntity.SetProgramId(null);

        _dbContext.CoursePrograms.Remove(program);
        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
