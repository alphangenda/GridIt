using System.Linq;
using Domain.Entities.Classes;
using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Npgsql;
using IMapper = AutoMapper.IMapper;

namespace Web.Features.Members.Classes.CreateExam;

public class CreateExamEndpoint : Endpoint<CreateExamRequest, ExamDto>
{
    private readonly IMapper _mapper;
    private readonly IExamRepository _examRepository;
    private readonly IClassSkillRepository _classSkillRepository;
    private readonly IConfiguration _configuration;

    public CreateExamEndpoint(
        IMapper mapper,
        IExamRepository examRepository,
        IClassSkillRepository classSkillRepository,
        IConfiguration configuration)
    {
        _mapper = mapper;
        _examRepository = examRepository;
        _classSkillRepository = classSkillRepository;
        _configuration = configuration;
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

        // Copier automatiquement les compétences de la classe vers l'examen
        var skillIds = await _classSkillRepository.GetSkillIdsForClass(req.ClassId);
        if (skillIds.Any())
        {
          await InsertExamSkills(exam.Id, skillIds);
        }

        await Send.OkAsync(_mapper.Map<ExamDto>(exam), ct);
    }

    private async Task InsertExamSkills(Guid examId, IEnumerable<Guid> skillIds)
    {
        var cs = _configuration.GetConnectionString("DefaultConnection");
        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();

        foreach (var skillId in skillIds.Distinct())
        {
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO exam_skills (id, exam_id, skill_id, position)
                VALUES (gen_random_uuid(), @ExamId, @SkillId,
                        (SELECT COALESCE(MAX(position), 0) + 1 FROM exam_skills WHERE exam_id = @ExamId))
                ON CONFLICT DO NOTHING;
                """,
                conn
            );

            cmd.Parameters.AddWithValue("@ExamId", examId);
            cmd.Parameters.AddWithValue("@SkillId", skillId);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
