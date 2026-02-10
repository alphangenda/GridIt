using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using IMapper = AutoMapper.IMapper;

namespace Web.Features.Members.Classes.GetExamsByClass;

public class GetExamsByClassEndpoint : Endpoint<GetExamsByClassRequest, List<ExamDto>>
{
    private readonly IMapper _mapper;
    private readonly IExamRepository _examRepository;

    public GetExamsByClassEndpoint(IMapper mapper, IExamRepository examRepository)
    {
        _mapper = mapper;
        _examRepository = examRepository;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Get("classes/{classId}/exams");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(GetExamsByClassRequest request, CancellationToken ct)
    {
        var exams = _examRepository.GetByClassId(request.ClassId);
        await Send.OkAsync(_mapper.Map<List<ExamDto>>(exams), ct);
    }
}
