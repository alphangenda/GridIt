using Domain.Entities.Classes;
using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using IMapper = AutoMapper.IMapper;

namespace Web.Features.Members.Classes.CreateExam;

public class CreateExamEndpoint : Endpoint<CreateExamRequest, ExamDto>
{
    private readonly IMapper _mapper;
    private readonly IExamRepository _examRepository;

    public CreateExamEndpoint(IMapper mapper, IExamRepository examRepository)
    {
        _mapper = mapper;
        _examRepository = examRepository;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Post("classes/{classId}/exams");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CreateExamRequest req, CancellationToken ct)
    {
        var exam = new Exam();
        exam.SetId(Guid.NewGuid());
        exam.SetClassId(req.ClassId);
        exam.SetName(req.Name.Trim());
        await _examRepository.CreateExam(exam);
        await Send.OkAsync(_mapper.Map<ExamDto>(exam), ct);
    }
}
