using Domain.Entities.Classes;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Web.Features.Admins.Programs.CreateProgram;

public class CreateProgramEndpoint : Endpoint<CreateProgramRequest, ProgramDto>
{
    private readonly GarneauTemplateDbContext _dbContext;

    public CreateProgramEndpoint(GarneauTemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Post("programs");
        Roles(Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CreateProgramRequest req, CancellationToken ct)
    {
        var name = req.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            AddError("Name", "Program name is required.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var exists = await _dbContext.CoursePrograms
            .AsNoTracking()
            .AnyAsync(p => p.Name.ToLower() == name.ToLower(), ct);
        if (exists)
        {
            ThrowError("A program with this name already exists.");
            return;
        }

        var program = new CourseProgram();
        program.SetId(Guid.NewGuid());
        program.SetName(name);

        _dbContext.CoursePrograms.Add(program);
        await _dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new ProgramDto
        {
            Id = program.Id,
            Name = program.Name
        }, ct);
    }
}
