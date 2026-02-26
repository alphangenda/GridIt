using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

        using var conn = new SqlConnection(cs);
        conn.Open();

        var cmd = new SqlCommand(@"
            SELECT s.Id, s.Label, es.Position
            FROM ExamSkills es
            JOIN Skills s ON s.Id = es.SkillId
            WHERE es.ExamId = @examId
            ORDER BY es.Position
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

        using var conn = new SqlConnection(cs);
        conn.Open();

        var cmd = new SqlCommand(@"
            INSERT INTO ExamSkills (Id, ExamId, SkillId, Position)
            VALUES (
                NEWID(),
                @examId,
                @skillId,
                (SELECT ISNULL(MAX(Position), 0) + 1 FROM ExamSkills WHERE ExamId = @examId)
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

        using var conn = new SqlConnection(cs);
        conn.Open();

        var cmd = new SqlCommand(@"
            DELETE FROM ExamSkills
            WHERE ExamId = @examId AND SkillId = @skillId
        ", conn);

        cmd.Parameters.AddWithValue("@examId", examId);
        cmd.Parameters.AddWithValue("@skillId", skillId);

        cmd.ExecuteNonQuery();

        return NoContent();
    }
}