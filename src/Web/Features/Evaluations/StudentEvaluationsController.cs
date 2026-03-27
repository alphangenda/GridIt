using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

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

    private static void EnsureTables(SqlConnection conn)
    {
        var cmd = new SqlCommand(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StudentEvaluations' AND xtype='U')
            BEGIN
                CREATE TABLE StudentEvaluations (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    ExamId UNIQUEIDENTIFIER NOT NULL,
                    StudentId NVARCHAR(255) NOT NULL,
                    CompetencyId NVARCHAR(255) NOT NULL,
                    Grade CHAR(1) NULL,
                    Comment NVARCHAR(MAX) NULL,
                    CONSTRAINT UQ_StudentEval UNIQUE (ExamId, StudentId, CompetencyId)
                )
            END

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StudentCriterionEvaluations' AND xtype='U')
            BEGIN
                CREATE TABLE StudentCriterionEvaluations (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    ExamId UNIQUEIDENTIFIER NOT NULL,
                    StudentId NVARCHAR(255) NOT NULL,
                    CriterionId UNIQUEIDENTIFIER NOT NULL,
                    Grade CHAR(1) NULL,
                    Comment NVARCHAR(MAX) NULL,
                    CONSTRAINT UQ_StudentCritEval UNIQUE (ExamId, StudentId, CriterionId)
                )
            END

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StudentExamVideos' AND xtype='U')
            BEGIN
                CREATE TABLE StudentExamVideos (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    ExamId UNIQUEIDENTIFIER NOT NULL,
                    StudentId NVARCHAR(255) NOT NULL,
                    VideoUrl NVARCHAR(500) NULL,
                    CONSTRAINT UQ_StudentExamVideo UNIQUE (ExamId, StudentId)
                )
            END
        ", conn);

        cmd.ExecuteNonQuery();
    }

    // GET /api/exams/{examId}/evaluations
    [HttpGet]
    public async Task<IActionResult> Get(Guid examId, CancellationToken ct)
    {
        var connStr = _config.GetConnectionString("DefaultConnection");
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);
        EnsureTables(conn);

        var compEvals = new List<object>();
        var critEvals = new List<object>();

        // Competency-level evaluations
        var cmd1 = new SqlCommand(@"
            SELECT StudentId, CompetencyId, Grade, Comment
            FROM StudentEvaluations
            WHERE ExamId = @examId
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
        var cmd2 = new SqlCommand(@"
            SELECT StudentId, CriterionId, Grade, Comment
            FROM StudentCriterionEvaluations
            WHERE ExamId = @examId
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
        var cmd3 = new SqlCommand(@"
            SELECT StudentId, VideoUrl
            FROM StudentExamVideos
            WHERE ExamId = @examId
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
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);
        EnsureTables(conn);

        using var tx = conn.BeginTransaction();

        // Upsert competency evaluations
        if (req.CompetencyEvaluations != null)
        {
            foreach (var e in req.CompetencyEvaluations)
            {
                var cmd = new SqlCommand(@"
                    MERGE StudentEvaluations AS target
                    USING (SELECT @examId AS ExamId, @studentId AS StudentId, @compId AS CompetencyId) AS source
                    ON target.ExamId = source.ExamId
                       AND target.StudentId = source.StudentId
                       AND target.CompetencyId = source.CompetencyId
                    WHEN MATCHED THEN
                        UPDATE SET Grade = @grade, Comment = @comment
                    WHEN NOT MATCHED THEN
                        INSERT (Id, ExamId, StudentId, CompetencyId, Grade, Comment)
                        VALUES (NEWID(), @examId, @studentId, @compId, @grade, @comment);
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
                var cmd = new SqlCommand(@"
                    MERGE StudentCriterionEvaluations AS target
                    USING (SELECT @examId AS ExamId, @studentId AS StudentId, @critId AS CriterionId) AS source
                    ON target.ExamId = source.ExamId
                       AND target.StudentId = source.StudentId
                       AND target.CriterionId = source.CriterionId
                    WHEN MATCHED THEN
                        UPDATE SET Grade = @grade, Comment = @comment
                    WHEN NOT MATCHED THEN
                        INSERT (Id, ExamId, StudentId, CriterionId, Grade, Comment)
                        VALUES (NEWID(), @examId, @studentId, @critId, @grade, @comment);
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
                var cmd = new SqlCommand(@"
                    MERGE StudentExamVideos AS target
                    USING (SELECT @examId AS ExamId, @studentId AS StudentId) AS source
                    ON target.ExamId = source.ExamId
                       AND target.StudentId = source.StudentId
                    WHEN MATCHED THEN
                        UPDATE SET VideoUrl = @videoUrl
                    WHEN NOT MATCHED THEN
                        INSERT (Id, ExamId, StudentId, VideoUrl)
                        VALUES (NEWID(), @examId, @studentId, @videoUrl);
                ", conn, tx);

                cmd.Parameters.AddWithValue("@examId", examId);
                cmd.Parameters.AddWithValue("@studentId", v.StudentId);
                cmd.Parameters.AddWithValue("@videoUrl", (object?)v.VideoUrl ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync(ct);
            }
        }

        tx.Commit();
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
