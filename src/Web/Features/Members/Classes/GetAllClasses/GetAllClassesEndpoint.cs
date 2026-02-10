using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Persistence.Extensions;
using IMapper = AutoMapper.IMapper;

namespace Web.Features.Members.Classes.GetAllClasses;

public class GetAllClassesEndpoint : EndpointWithoutRequest<List<ClassDto>>
{
    private readonly IMapper _mapper;
    private readonly IClassRepository _classRepository;

    public GetAllClassesEndpoint(IMapper mapper, IClassRepository classRepository)
    {
        _mapper = mapper;
        _classRepository = classRepository;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Get("classes");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userEmail = HttpContext.GetUserEmail() ?? "";
        var classes = _classRepository.GetAllByUser(userEmail);
        await Send.OkAsync(_mapper.Map<List<ClassDto>>(classes), ct);
    }
}
