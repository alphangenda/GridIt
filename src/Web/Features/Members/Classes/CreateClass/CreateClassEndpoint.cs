using Domain.Entities.Classes;
using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Persistence;
using IMapper = AutoMapper.IMapper;

namespace Web.Features.Members.Classes.CreateClass;

public class CreateClassEndpoint : Endpoint<CreateClassRequest, ClassDto>
{
    private readonly IMapper _mapper;
    private readonly IClassRepository _classRepository;
    private readonly IClassSkillRepository _classSkillRepository;
    private readonly GarneauTemplateDbContext _dbContext;

    public CreateClassEndpoint(
        IMapper mapper,
        IClassRepository classRepository,
        IClassSkillRepository classSkillRepository,
        GarneauTemplateDbContext dbContext)
    {
        _mapper = mapper;
        _classRepository = classRepository;
        _classSkillRepository = classSkillRepository;
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Post("classes");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CreateClassRequest req, CancellationToken ct)
    {
        if (req.ProgramId.HasValue)
        {
            var exists = await _dbContext.CoursePrograms
                .AsNoTracking()
                .AnyAsync(x => x.Id == req.ProgramId.Value, ct);
            if (!exists)
            {
                AddError("ProgramId", "Program not found.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }
        }

        var classEntity = new Class();
        classEntity.SetName(req.Name.Trim());
        classEntity.SetId(Guid.NewGuid());
        classEntity.SetProgramId(req.ProgramId);

        await _classRepository.CreateClass(classEntity);

        if (req.SkillIds is not null && req.SkillIds.Count > 0)
        {
            await _classSkillRepository.SaveClassSkills(classEntity.Id, req.SkillIds);
        }

        await Send.OkAsync(_mapper.Map<ClassDto>(classEntity), ct);
    }
}