using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Web.Dtos;

namespace Web.Features.Skills;

[ApiController]
[Route("api/skills")]
public class SkillsController : ControllerBase
{
    private readonly IConfiguration _config;

    public SkillsController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var skills = new List<SkillDto>();
        var connectionString = _config.GetConnectionString("DefaultConnection");

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT id, label FROM skills",
            conn
        );

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            skills.Add(new SkillDto
            {
                Id = reader.GetGuid(0),
                Label = reader.GetString(1)
            });
        }

        return Ok(skills);
    }

    [HttpGet("{skillId}/subcompetencies")]
    public IActionResult GetSubcompetencies(Guid skillId)
    {
        var subcompetencies = new List<object>();
        var connectionString = _config.GetConnectionString("DefaultConnection");

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand(@"
            SELECT id, skill_id, label, position
            FROM  skill_subskills
            WHERE skill_id = @skillId
            ORDER BY position
        ", conn);

        cmd.Parameters.AddWithValue("skillId", skillId);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            subcompetencies.Add(new
            {
                id = reader.GetGuid(0),
                skillId = reader.GetGuid(1),
                label = reader.GetString(2),
                position = reader.GetInt32(3)
            });
        }

        return Ok(subcompetencies);
    }

    [HttpPost("{skillId}/criteria-template")]
    public async Task<IActionResult> SaveCriteriaTemplate(
        Guid skillId,
        [FromBody] SaveCriteriaTemplateRequest req,
        CancellationToken ct)
    {
        try
        {
            if (req.Templates == null || req.Templates.Count == 0)
                return NoContent();

            var connectionString = _config.GetConnectionString("DefaultConnection");

            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);

            var ensureTable = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS skill_criteria_template (
                    id uuid PRIMARY KEY,
                    skill_id uuid NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
                    label text NOT NULL,
                    default_total_value integer NOT NULL DEFAULT 15,
                    position integer NOT NULL DEFAULT 0
                )", conn);
            await ensureTable.ExecuteNonQueryAsync(ct);

            using var tx = await conn.BeginTransactionAsync(ct);

            var delete = new NpgsqlCommand(
                "DELETE FROM skill_criteria_template WHERE skill_id = @skillId",
                conn, tx);
            delete.Parameters.AddWithValue("skillId", skillId);
            await delete.ExecuteNonQueryAsync(ct);

            for (var i = 0; i < req.Templates.Count; i++)
            {
                var t = req.Templates[i];
                if (string.IsNullOrWhiteSpace(t.Label)) continue;

                var insert = new NpgsqlCommand(@"
                    INSERT INTO skill_criteria_template (id, skill_id, label, default_total_value, position)
                    VALUES (@id, @skillId, @label, @val, @pos)",
                    conn, tx);

                insert.Parameters.AddWithValue("id", Guid.NewGuid());
                insert.Parameters.AddWithValue("skillId", skillId);
                insert.Parameters.AddWithValue("label", t.Label.Trim());
                insert.Parameters.AddWithValue("val", t.DefaultTotalValue > 0 ? t.DefaultTotalValue : 15);
                insert.Parameters.AddWithValue("pos", i);

                await insert.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(
                title: ex.GetType().Name,
                detail: ex.Message,
                statusCode: 500
            );
        }
    }
}

public class SaveCriteriaTemplateRequest
{
    public List<CriteriaTemplateItem> Templates { get; set; } = new();
}

public class CriteriaTemplateItem
{
    public string Label { get; set; } = string.Empty;
    public int DefaultTotalValue { get; set; }
}