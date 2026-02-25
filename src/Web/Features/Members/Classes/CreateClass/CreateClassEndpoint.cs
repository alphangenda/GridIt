using Domain.Entities.Classes;
using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Persistence;
using IMapper = AutoMapper.IMapper;

namespace Web.Features.Members.Classes.CreateClass;

public class CreateClassEndpoint : Endpoint<CreateClassRequest, ClassDto>
{
    private readonly IMapper _mapper;
    private readonly IClassRepository _classRepository;
    private readonly GarneauTemplateDbContext _context;

    public CreateClassEndpoint(IMapper mapper, IClassRepository classRepository, GarneauTemplateDbContext context)
    {
        _mapper = mapper;
        _classRepository = classRepository;
        _context = context;
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
        var classEntity = new Class();
        classEntity.SetName(req.Name.Trim());
        classEntity.SetId(Guid.NewGuid());

        if (req.Students is { Count: > 0 })
        {
            foreach (var s in req.Students)
            {
                var student = new Student();
                student.SetId(Guid.NewGuid());
                student.SetClassId(classEntity.Id);
                student.SetNumber(s.Number.Trim());
                student.SetFirstName(s.FirstName.Trim());
                student.SetLastName(s.LastName.Trim());
                _context.Students.Add(student);
            }
        }

        await _classRepository.CreateClass(classEntity);
        await Send.OkAsync(_mapper.Map<ClassDto>(classEntity), ct);
    }
}
