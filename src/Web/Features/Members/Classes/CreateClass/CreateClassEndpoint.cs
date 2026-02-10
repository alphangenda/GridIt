using Domain.Entities.Classes;
using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using IMapper = AutoMapper.IMapper;

namespace Web.Features.Members.Classes.CreateClass;

public class CreateClassEndpoint : Endpoint<CreateClassRequest, ClassDto>
{
    private readonly IMapper _mapper;
    private readonly IClassRepository _classRepository;

    public CreateClassEndpoint(IMapper mapper, IClassRepository classRepository)
    {
        _mapper = mapper;
        _classRepository = classRepository;
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
        await _classRepository.CreateClass(classEntity);
        await Send.OkAsync(_mapper.Map<ClassDto>(classEntity), ct);
    }
}
