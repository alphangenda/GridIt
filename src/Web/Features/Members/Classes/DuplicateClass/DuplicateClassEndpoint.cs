using Domain.Entities.Classes;
using Domain.Repositories;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Persistence;
using Web.Features.Members.Classes;
using IMapper = AutoMapper.IMapper;

namespace Web.Features.Members.Classes.DuplicateClass;

public class DuplicateClassEndpoint : Endpoint<DuplicateClassRequest, ClassDto>
{
    private readonly IMapper _mapper;
    private readonly GarneauTemplateDbContext _context;
    private readonly IClassRepository _classRepository;
    private readonly IClassSkillRepository _classSkillRepository;
    private readonly IExamRepository _examRepository;
    private readonly IConfiguration _configuration;

    public DuplicateClassEndpoint(
        IMapper mapper,
        GarneauTemplateDbContext context,
        IClassRepository classRepository,
        IClassSkillRepository classSkillRepository,
        IExamRepository examRepository,
        IConfiguration configuration)
    {
        _mapper = mapper;
        _context = context;
        _classRepository = classRepository;
        _classSkillRepository = classSkillRepository;
        _examRepository = examRepository;
        _configuration = configuration;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Post("classes/duplicate");
        Roles(Domain.Constants.User.Roles.MEMBER, Domain.Constants.User.Roles.ADMINISTRATOR);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(DuplicateClassRequest req, CancellationToken ct)
    {
        // Validate source class exists
        var sourceClass = await _context.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.SourceClassId, ct);

        if (sourceClass == null)
        {
            AddError("SourceClassId", "Source class not found.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (req.ProgramId.HasValue)
        {
            var exists = await _context.CoursePrograms
                .AsNoTracking()
                .AnyAsync(x => x.Id == req.ProgramId.Value, ct);
            if (!exists)
            {
                AddError("ProgramId", "Program not found.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }
        }

        // 1. Create the new class
        var newClass = new Class();
        newClass.SetId(Guid.NewGuid());
        newClass.SetName(req.Name.Trim());
        newClass.SetProgramId(req.ProgramId);
        await _classRepository.CreateClass(newClass);

        // 2. Copy skills from source class
        var sourceSkillIds = await _classSkillRepository.GetSkillIdsForClass(req.SourceClassId);
        var skillIdsList = sourceSkillIds.ToList();

        if (skillIdsList.Count > 0)
        {
            await _classSkillRepository.SaveClassSkills(newClass.Id, skillIdsList);
        }

        // 3. Copy exams with custom names
        var sourceExams = _examRepository.GetByClassId(req.SourceClassId);
        var renameMap = req.Exams.ToDictionary(e => e.SourceExamId, e => e.Name);
        var cs = _configuration.GetConnectionString("DefaultConnection");

        foreach (var sourceExam in sourceExams)
        {
            // Use custom name if provided, otherwise keep original
            var examName = renameMap.TryGetValue(sourceExam.Id, out var customName)
                && !string.IsNullOrWhiteSpace(customName)
                    ? customName.Trim()
                    : sourceExam.Name;

            var newExam = new Exam();
            newExam.SetId(Guid.NewGuid());
            newExam.SetClassId(newClass.Id);
            newExam.SetName(examName);
            await _examRepository.CreateExam(newExam);

            // Copy exam_skills with positions
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync(ct);

            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO exam_skills (id, exam_id, skill_id, position)
                SELECT gen_random_uuid(), @NewExamId, skill_id, position
                FROM exam_skills
                WHERE exam_id = @SourceExamId
                """,
                conn
            );

            cmd.Parameters.AddWithValue("@NewExamId", newExam.Id);
            cmd.Parameters.AddWithValue("@SourceExamId", sourceExam.Id);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        await Send.OkAsync(_mapper.Map<ClassDto>(newClass), ct);
    }
}
