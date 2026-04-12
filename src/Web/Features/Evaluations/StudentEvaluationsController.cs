using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Web.Features.Evaluations;

[ApiController]
[Route("api/exams/{examId}/evaluations")]
public class StudentEvaluationsController : ControllerBase
{
    private readonly IConfiguration _config;

    public StudentEvaluationsController(IConfiguration config)
    {
        _config = config;
    }

    private static void EnsureTables(NpgsqlConnection conn)
    {
        var cmd = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS student_evaluations (
                id UUID NOT NULL PRIMARY KEY,
                exam_id UUID NOT NULL,
                student_id VARCHAR(255) NOT NULL,
                competency_id VARCHAR(255) NOT NULL,
                grade CHAR(1) NULL,
                comment TEXT NULL,
                CONSTRAINT uq_student_eval UNIQUE (exam_id, student_id, competency_id)
            );

            CREATE TABLE IF NOT EXISTS student_criterion_evaluations (
                id UUID NOT NULL PRIMARY KEY,
                exam_id UUID NOT NULL,
                student_id VARCHAR(255) NOT NULL,
                criterion_id UUID NOT NULL,
                grade CHAR(1) NULL,
                comment TEXT NULL,
                CONSTRAINT uq_student_crit_eval UNIQUE (exam_id, student_id, criterion_id)
            );

            CREATE TABLE IF NOT EXISTS student_exam_videos (
                id UUID NOT NULL PRIMARY KEY,
                exam_id UUID NOT NULL,
                student_id VARCHAR(255) NOT NULL,
                video_url VARCHAR(500) NULL,
                CONSTRAINT uq_student_exam_video UNIQUE (exam_id, student_id)
            );
        ", conn);

        cmd.ExecuteNonQuery();
    }

    // GET /api/exams/{examId}/evaluations
    [HttpGet]
    public async Task<IActionResult> Get(Guid examId, CancellationToken ct)
    {
        var connStr = _config.GetConnectionString("DefaultConnection");
        using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync(ct);
        EnsureTables(conn);

        var compEvals = new List<object>();
        var critEvals = new List<object>();

        // Competency-level evaluations
        var cmd1 = new NpgsqlCommand(@"
            SELECT student_id, competency_id, grade, comment
            FROM student_evaluations
            WHERE exam_id = @examId
        ", conn);
        cmd1.Parameters.AddWithValue("@examId", examId);

        using (var reader = await cmd1.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                compEvals.Add(new
                {
                    studentId = reader.GetString(0),
                    competencyId = reader.GetString(1),
                    grade = reader.IsDBNull(2) ? null : reader.GetString(2).Trim(),
                    comment = reader.IsDBNull(3) ? "" : reader.GetString(3)
                });
            }
        }

        // Criterion-level evaluations
        var cmd2 = new NpgsqlCommand(@"
            SELECT student_id, criterion_id, grade, comment
            FROM student_criterion_evaluations
            WHERE exam_id = @examId
        ", conn);
        cmd2.Parameters.AddWithValue("@examId", examId);

        using (var reader = await cmd2.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                critEvals.Add(new
                {
                    studentId = reader.GetString(0),
                    criterionId = reader.GetGuid(1).ToString(),
                    grade = reader.IsDBNull(2) ? null : reader.GetString(2).Trim(),
                    comment = reader.IsDBNull(3) ? "" : reader.GetString(3)
                });
            }
        }

        // Video URLs
        var videoUrls = new List<object>();
        var cmd3 = new NpgsqlCommand(@"
            SELECT student_id, video_url
            FROM student_exam_videos
            WHERE exam_id = @examId
        ", conn);
        cmd3.Parameters.AddWithValue("@examId", examId);

        using (var reader = await cmd3.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                videoUrls.Add(new
                {
                    studentId = reader.GetString(0),
                    videoUrl = reader.IsDBNull(1) ? "" : reader.GetString(1)
                });
            }
        }

        return Ok(new { competencyEvaluations = compEvals, criterionEvaluations = critEvals, videoUrls });
    }

    // POST /api/exams/{examId}/evaluations
    [HttpPost]
    public async Task<IActionResult> Save(
        Guid examId,
        [FromBody] SaveEvaluationsRequest req,
        CancellationToken ct)
    {
        var connStr = _config.GetConnectionString("DefaultConnection");
        using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync(ct);
        EnsureTables(conn);

        using var tx = await conn.BeginTransactionAsync(ct);

        // Upsert competency evaluations
        if (req.CompetencyEvaluations != null)
        {
            foreach (var e in req.CompetencyEvaluations)
            {
                var cmd = new NpgsqlCommand(@"
                    INSERT INTO student_evaluations (id, exam_id, student_id, competency_id, grade, comment)
                    VALUES (gen_random_uuid(), @examId, @studentId, @compId, @grade, @comment)
                    ON CONFLICT (exam_id, student_id, competency_id) DO UPDATE SET
                        grade = EXCLUDED.grade,
                        comment = EXCLUDED.comment
                ", conn, tx);

                cmd.Parameters.AddWithValue("@examId", examId);
                cmd.Parameters.AddWithValue("@studentId", e.StudentId);
                cmd.Parameters.AddWithValue("@compId", e.CompetencyId);
                cmd.Parameters.AddWithValue("@grade", (object?)e.Grade ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@comment", (object?)e.Comment ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync(ct);
            }
        }

        // Upsert criterion evaluations
        if (req.CriterionEvaluations != null)
        {
            foreach (var e in req.CriterionEvaluations)
            {
                var cmd = new NpgsqlCommand(@"
                    INSERT INTO student_criterion_evaluations (id, exam_id, student_id, criterion_id, grade, comment)
                    VALUES (gen_random_uuid(), @examId, @studentId, @critId, @grade, @comment)
                    ON CONFLICT (exam_id, student_id, criterion_id) DO UPDATE SET
                        grade = EXCLUDED.grade,
                        comment = EXCLUDED.comment
                ", conn, tx);

                cmd.Parameters.AddWithValue("@examId", examId);
                cmd.Parameters.AddWithValue("@studentId", e.StudentId);
                cmd.Parameters.AddWithValue("@critId", Guid.Parse(e.CriterionId));
                cmd.Parameters.AddWithValue("@grade", (object?)e.Grade ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@comment", (object?)e.Comment ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync(ct);
            }
        }

        // Upsert video URLs
        if (req.VideoUrls != null)
        {
            foreach (var v in req.VideoUrls)
            {
                var cmd = new NpgsqlCommand(@"
                    INSERT INTO student_exam_videos (id, exam_id, student_id, video_url)
                    VALUES (gen_random_uuid(), @examId, @studentId, @videoUrl)
                    ON CONFLICT (exam_id, student_id) DO UPDATE SET
                        video_url = EXCLUDED.video_url
                ", conn, tx);

                cmd.Parameters.AddWithValue("@examId", examId);
                cmd.Parameters.AddWithValue("@studentId", v.StudentId);
                cmd.Parameters.AddWithValue("@videoUrl", (object?)v.VideoUrl ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync(ct);
            }
        }

        await tx.CommitAsync(ct);
        return NoContent();
    }
}

public class SaveEvaluationsRequest
{
    public List<CompetencyEvalPayload>? CompetencyEvaluations { get; set; }
    public List<CriterionEvalPayload>? CriterionEvaluations { get; set; }
    public List<VideoUrlPayload>? VideoUrls { get; set; }
}

public class CompetencyEvalPayload
{
    public string StudentId { get; set; } = "";
    public string CompetencyId { get; set; } = "";
    public string? Grade { get; set; }
    public string? Comment { get; set; }
}

public class CriterionEvalPayload
{
    public string StudentId { get; set; } = "";
    public string CriterionId { get; set; } = "";
    public string? Grade { get; set; }
    public string? Comment { get; set; }
}

public class VideoUrlPayload
{
    public string StudentId { get; set; } = "";
    public string? VideoUrl { get; set; }
}
