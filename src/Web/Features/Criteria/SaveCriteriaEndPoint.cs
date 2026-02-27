using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Web.Dtos;

namespace Web.Features.Criteria;

[ApiController]
[Route("api/exams/{examId}/skills/{skillId}/criteria")]
public class SaveCriteriaController : ControllerBase
{
    private readonly IConfiguration _config;

    public SaveCriteriaController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        Guid examId,
        Guid skillId,
        CancellationToken ct)
    {
        var connStr = _config.GetConnectionString("DefaultConnection");
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        var getExamSkillId = new SqlCommand(@"
            SELECT Id
            FROM ExamSkills
            WHERE ExamId = @examId AND SkillId = @skillId
        ", conn);

        getExamSkillId.Parameters.AddWithValue("@examId", examId);
        getExamSkillId.Parameters.AddWithValue("@skillId", skillId);

        var examSkillIdObj = await getExamSkillId.ExecuteScalarAsync(ct);
        if (examSkillIdObj == null)
            return Ok(Array.Empty<CriterionDto>());

        var examSkillId = (Guid)examSkillIdObj;

        var cmd = new SqlCommand(@"
            SELECT
                c.Id,
                c.Label,
                c.TotalValue,
                c.Position,
                w.Weight,
                w.Value,
                w.Description,
                w.IsEnabled
            FROM Criteria c
            LEFT JOIN CriterionWeights w ON w.CriterionId = c.Id
            WHERE c.ExamSkillId = @examSkillId
            ORDER BY c.Position
        ", conn);

        cmd.Parameters.AddWithValue("@examSkillId", examSkillId);

        using var reader = await cmd.ExecuteReaderAsync(ct);

        var map = new Dictionary<Guid, CriterionDto>();

        while (await reader.ReadAsync(ct))
        {
            var cid = reader.GetGuid(0);

            if (!map.TryGetValue(cid, out var c))
            {
                c = new CriterionDto
                {
                    Id = cid,
                    Label = reader.GetString(1),
                    TotalValue = reader.GetInt32(2),
                    Position = reader.GetInt32(3),
                    Weights = new List<CriterionWeightDto>()
                };
                map[cid] = c;
            }

            if (!reader.IsDBNull(4))
            {
                c.Weights.Add(new CriterionWeightDto
                {
                    Weight = reader.GetString(4),
                    Value = reader.GetInt32(5),
                    Description = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    IsEnabled = reader.GetBoolean(7)
                });
            }
        }

        return Ok(map.Values);
    }


    [HttpDelete("{criterionId}")]
    public async Task<IActionResult> DeleteCriterion(
        Guid examId,
        Guid skillId,
        Guid criterionId,
        CancellationToken ct)
    {
        var connStr = _config.GetConnectionString("DefaultConnection");
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        var cmd = new SqlCommand(@"
            DELETE FROM CriterionWeights WHERE CriterionId = @id;
            DELETE FROM Criteria WHERE Id = @id;
        ", conn);

        cmd.Parameters.AddWithValue("@id", criterionId);
        await cmd.ExecuteNonQueryAsync(ct);

        return NoContent();
    }

[HttpPost]
public async Task<IActionResult> Save(
    Guid examId,
    Guid skillId,
    SaveCriteriaRequest req,
    CancellationToken ct)
{
    try
    {
        if (req.Criteria == null || req.Criteria.Count == 0)
            return NoContent();

        var validCriteria = req.Criteria
            .Where(c => !string.IsNullOrWhiteSpace(c.Label) && c.TotalValue > 0)
            .ToList();

        if (validCriteria.Count == 0)
            return NoContent();

        var connStr = _config.GetConnectionString("DefaultConnection");
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        var getExamSkillId = new SqlCommand(@"
            SELECT Id
            FROM ExamSkills
            WHERE ExamId = @examId AND SkillId = @skillId
        ", conn);

        getExamSkillId.Parameters.AddWithValue("@examId", examId);
        getExamSkillId.Parameters.AddWithValue("@skillId", skillId);

        var examSkillIdObj = await getExamSkillId.ExecuteScalarAsync(ct);

        if (examSkillIdObj == null)
            return BadRequest("ExamSkill not found.");

        var examSkillId = (Guid)examSkillIdObj;

        using var tx = conn.BeginTransaction();

        var delete = new SqlCommand(@"
            DELETE cw FROM CriterionWeights cw
            INNER JOIN Criteria c ON c.Id = cw.CriterionId
            WHERE c.ExamSkillId = @examSkillId;

            DELETE FROM Criteria WHERE ExamSkillId = @examSkillId;
        ", conn, tx);

        delete.Parameters.AddWithValue("@examSkillId", examSkillId);
        await delete.ExecuteNonQueryAsync(ct);

        foreach (var c in validCriteria)
        {
            var criterionId = Guid.NewGuid();

            var insertCriterion = new SqlCommand(@"
                INSERT INTO Criteria (Id, ExamSkillId, Label, TotalValue, Position)
                VALUES (@id, @examSkillId, @label, @total, @pos)",
                conn, tx);

            insertCriterion.Parameters.AddWithValue("@id", criterionId);
            insertCriterion.Parameters.AddWithValue("@examSkillId", examSkillId);
            insertCriterion.Parameters.AddWithValue("@label", c.Label);
            insertCriterion.Parameters.AddWithValue("@total", c.TotalValue);
            insertCriterion.Parameters.AddWithValue("@pos", c.Position);

            await insertCriterion.ExecuteNonQueryAsync(ct);

            foreach (var w in c.Weights.Where(w => w.IsEnabled && w.Value > 0))
            {
                var insertWeight = new SqlCommand(@"
                    INSERT INTO CriterionWeights
                    (Id, CriterionId, Weight, Value, Description, IsEnabled)
                    VALUES (@id, @cid, @w, @val, @desc, @en)",
                    conn, tx);

                insertWeight.Parameters.AddWithValue("@id", Guid.NewGuid());
                insertWeight.Parameters.AddWithValue("@cid", criterionId);
                insertWeight.Parameters.AddWithValue("@w", w.Weight);
                insertWeight.Parameters.AddWithValue("@val", w.Value);
                insertWeight.Parameters.AddWithValue("@desc", (object?)w.Description ?? DBNull.Value);
                insertWeight.Parameters.AddWithValue("@en", true);

                await insertWeight.ExecuteNonQueryAsync(ct);
            }
        }

        tx.Commit();
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