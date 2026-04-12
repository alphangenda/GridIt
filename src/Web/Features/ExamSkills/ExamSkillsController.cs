using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Web.Dtos;

namespace Web.Features.ExamSkills;

[ApiController]
[Route("api/exams/{examId}/skills")]
public class ExamSkillsController : ControllerBase
{
    private readonly IConfiguration _config;

    public ExamSkillsController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public IActionResult Get(Guid examId)
    {
        var result = new List<ExamSkillDto>();
        var cs = _config.GetConnectionString("DefaultConnection");

        using var conn = new NpgsqlConnection(cs);
        conn.Open();

        var cmd = new NpgsqlCommand(@"
            SELECT s.id, s.label, es.position
            FROM exam_skills es
            JOIN skills s ON s.id = es.skill_id
            WHERE es.exam_id = @examId
            ORDER BY es.position
        ", conn);

        cmd.Parameters.AddWithValue("@examId", examId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ExamSkillDto
            {
                SkillId = reader.GetGuid(0),
                Label = reader.GetString(1),
                Position = reader.GetInt32(2)
            });
        }

        return Ok(result);
    }

    [HttpPost]
    public IActionResult Add(Guid examId, AddExamSkillDto dto)
    {
        var cs = _config.GetConnectionString("DefaultConnection");

        using var conn = new NpgsqlConnection(cs);
        conn.Open();

        var cmd = new NpgsqlCommand(@"
            INSERT INTO exam_skills (id, exam_id, skill_id, position)
            VALUES (
                gen_random_uuid(),
                @examId,
                @skillId,
                (SELECT COALESCE(MAX(position), 0) + 1 FROM exam_skills WHERE exam_id = @examId)
            )
        ", conn);

        cmd.Parameters.AddWithValue("@examId", examId);
        cmd.Parameters.AddWithValue("@skillId", dto.SkillId);

        cmd.ExecuteNonQuery();

        return Ok();
    }

    [HttpDelete("{skillId}")]
    public IActionResult Remove(Guid examId, Guid skillId)
    {
        var cs = _config.GetConnectionString("DefaultConnection");

        using var conn = new NpgsqlConnection(cs);
        conn.Open();

        var cmd = new NpgsqlCommand(@"
            DELETE FROM exam_skills
            WHERE exam_id = @examId AND skill_id = @skillId
        ", conn);

        cmd.Parameters.AddWithValue("@examId", examId);
        cmd.Parameters.AddWithValue("@skillId", skillId);

        cmd.ExecuteNonQuery();

        return NoContent();
    }
}
