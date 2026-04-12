using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Web.Dtos;

namespace Web.Features.Members.Classes.ExamGroups;

[ApiController]
[Route("api/exams/{examId}/groups")]
public class ExamGroupsController : ControllerBase
{
    private readonly IConfiguration _config;

    public ExamGroupsController(IConfiguration config)
    {
        _config = config;
    }

    private static void EnsureTables(NpgsqlConnection conn)
    {
        using var cmd1 = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS exam_groups (
                id       UUID DEFAULT gen_random_uuid() PRIMARY KEY,
                exam_id  UUID NOT NULL,
                class_id UUID NOT NULL,
                name     VARCHAR(255) NOT NULL
            );

            CREATE TABLE IF NOT EXISTS exam_group_students (
                id         UUID DEFAULT gen_random_uuid() PRIMARY KEY,
                group_id   UUID NOT NULL,
                number     VARCHAR(100) NOT NULL,
                first_name VARCHAR(255) NOT NULL,
                last_name  VARCHAR(255) NOT NULL
            );
        ", conn);
        cmd1.ExecuteNonQuery();
    }

    [HttpGet]
    public IActionResult Get(Guid examId)
    {
        var cs = _config.GetConnectionString("DefaultConnection");
        using var conn = new NpgsqlConnection(cs);
        conn.Open();
        EnsureTables(conn);

        var groups = new List<ExamGroupDto>();

        var cmd = new NpgsqlCommand(@"
            SELECT g.id, g.name,
                   (SELECT COUNT(*) FROM exam_group_students s WHERE s.group_id = g.id) AS student_count
            FROM exam_groups g
            WHERE g.exam_id = @examId
            ORDER BY g.name
        ", conn);
        cmd.Parameters.AddWithValue("@examId", examId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            groups.Add(new ExamGroupDto
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                StudentCount = reader.GetInt32(2),
            });
        }

        return Ok(groups);
    }

    [HttpGet("{groupId}/students")]
    public IActionResult GetStudents(Guid examId, Guid groupId)
    {
        var cs = _config.GetConnectionString("DefaultConnection");
        using var conn = new NpgsqlConnection(cs);
        conn.Open();
        EnsureTables(conn);

        var students = new List<StudentDto>();

        var cmd = new NpgsqlCommand(@"
            SELECT s.id, s.number, s.first_name, s.last_name
            FROM exam_group_students s
            INNER JOIN exam_groups g ON g.id = s.group_id AND g.exam_id = @examId
            WHERE s.group_id = @groupId
            ORDER BY s.last_name, s.first_name
        ", conn);
        cmd.Parameters.AddWithValue("@examId", examId);
        cmd.Parameters.AddWithValue("@groupId", groupId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            students.Add(new StudentDto
            {
                Id = reader.GetGuid(0),
                Number = reader.GetString(1),
                FirstName = reader.GetString(2),
                LastName = reader.GetString(3),
            });
        }

        return Ok(students);
    }

    [HttpPost]
    public IActionResult Post(Guid examId, [FromBody] CreateExamGroupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Name is required.");

        var cs = _config.GetConnectionString("DefaultConnection");
        using var conn = new NpgsqlConnection(cs);
        conn.Open();
        EnsureTables(conn);

        var groupId = Guid.NewGuid();

        var insertGroup = new NpgsqlCommand(@"
            INSERT INTO exam_groups (id, exam_id, class_id, name) VALUES (@id, @examId, @classId, @name)
        ", conn);
        insertGroup.Parameters.AddWithValue("@id", groupId);
        insertGroup.Parameters.AddWithValue("@examId", examId);
        insertGroup.Parameters.AddWithValue("@classId", req.ClassId);
        insertGroup.Parameters.AddWithValue("@name", req.Name.Trim());
        insertGroup.ExecuteNonQuery();

        if (req.Students is { Count: > 0 })
        {
            foreach (var s in req.Students)
            {
                var insertStudent = new NpgsqlCommand(@"
                    INSERT INTO exam_group_students (id, group_id, number, first_name, last_name)
                    VALUES (gen_random_uuid(), @groupId, @number, @firstName, @lastName)
                ", conn);
                insertStudent.Parameters.AddWithValue("@groupId", groupId);
                insertStudent.Parameters.AddWithValue("@number", s.Number);
                insertStudent.Parameters.AddWithValue("@firstName", s.FirstName);
                insertStudent.Parameters.AddWithValue("@lastName", s.LastName);
                insertStudent.ExecuteNonQuery();
            }
        }

        return Ok(new { id = groupId });
    }

    [HttpDelete("{groupId}")]
    public IActionResult Delete(Guid examId, Guid groupId)
    {
        var cs = _config.GetConnectionString("DefaultConnection");
        using var conn = new NpgsqlConnection(cs);
        conn.Open();
        EnsureTables(conn);

        new NpgsqlCommand(@"DELETE FROM exam_group_students WHERE group_id = @groupId", conn)
            .Also(c => c.Parameters.AddWithValue("@groupId", groupId))
            .ExecuteNonQuery();

        new NpgsqlCommand(@"DELETE FROM exam_groups WHERE id = @groupId AND exam_id = @examId", conn)
            .Also(c => {
                c.Parameters.AddWithValue("@groupId", groupId);
                c.Parameters.AddWithValue("@examId", examId);
            })
            .ExecuteNonQuery();

        return Ok();
    }
}

internal static class NpgsqlCommandExtensions
{
    public static NpgsqlCommand Also(this NpgsqlCommand cmd, Action<NpgsqlCommand> configure)
    {
        configure(cmd);
        return cmd;
    }
}
