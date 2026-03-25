using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

    private static void EnsureTables(SqlConnection conn)
    {
        // Drop old ExamGroups table if it has the wrong schema (missing Name column)
        using var dropOld = new SqlCommand(@"
            IF EXISTS (SELECT * FROM sysobjects WHERE name='ExamGroups' AND xtype='U')
            AND NOT EXISTS (
                SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'ExamGroups' AND COLUMN_NAME = 'Name'
            )
            BEGIN
                IF EXISTS (SELECT * FROM sysobjects WHERE name='ExamGroupStudents' AND xtype='U')
                    DROP TABLE ExamGroupStudents
                DROP TABLE ExamGroups
            END
        ", conn);
        dropOld.ExecuteNonQuery();

        using var cmd1 = new SqlCommand(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ExamGroups' AND xtype='U')
            BEGIN
                CREATE TABLE ExamGroups (
                    Id       UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
                    ExamId   UNIQUEIDENTIFIER NOT NULL,
                    ClassId  UNIQUEIDENTIFIER NOT NULL,
                    Name     NVARCHAR(255)    NOT NULL
                )
            END
        ", conn);
        cmd1.ExecuteNonQuery();

        // Migration : ajouter ClassId si la table existe sans cette colonne
        using var addClassId = new SqlCommand(@"
            IF EXISTS (SELECT * FROM sysobjects WHERE name='ExamGroups' AND xtype='U')
            AND NOT EXISTS (
                SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'ExamGroups' AND COLUMN_NAME = 'ClassId'
            )
            BEGIN
                ALTER TABLE ExamGroups
                ADD ClassId UNIQUEIDENTIFIER NOT NULL
                DEFAULT '00000000-0000-0000-0000-000000000000'
            END
        ", conn);
        addClassId.ExecuteNonQuery();

        using var cmd2 = new SqlCommand(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ExamGroupStudents' AND xtype='U')
            BEGIN
                CREATE TABLE ExamGroupStudents (
                    Id          UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
                    GroupId     UNIQUEIDENTIFIER NOT NULL,
                    Number      NVARCHAR(100)    NOT NULL,
                    FirstName   NVARCHAR(255)    NOT NULL,
                    LastName    NVARCHAR(255)    NOT NULL
                )
            END
        ", conn);
        cmd2.ExecuteNonQuery();
    }

    [HttpGet]
    public IActionResult Get(Guid examId)
    {
        var cs = _config.GetConnectionString("DefaultConnection");
        using var conn = new SqlConnection(cs);
        conn.Open();
        EnsureTables(conn);

        var groups = new List<ExamGroupDto>();

        var cmd = new SqlCommand(@"
            SELECT g.Id, g.Name,
                   (SELECT COUNT(*) FROM ExamGroupStudents s WHERE s.GroupId = g.Id) AS StudentCount
            FROM ExamGroups g
            WHERE g.ExamId = @examId
            ORDER BY g.Name
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
        using var conn = new SqlConnection(cs);
        conn.Open();
        EnsureTables(conn);

        var students = new List<StudentDto>();

        var cmd = new SqlCommand(@"
            SELECT s.Id, s.Number, s.FirstName, s.LastName
            FROM ExamGroupStudents s
            INNER JOIN ExamGroups g ON g.Id = s.GroupId AND g.ExamId = @examId
            WHERE s.GroupId = @groupId
            ORDER BY s.LastName, s.FirstName
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
        using var conn = new SqlConnection(cs);
        conn.Open();
        EnsureTables(conn);

        var groupId = Guid.NewGuid();

        var insertGroup = new SqlCommand(@"
            INSERT INTO ExamGroups (Id, ExamId, ClassId, Name) VALUES (@id, @examId, @classId, @name)
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
                var insertStudent = new SqlCommand(@"
                    INSERT INTO ExamGroupStudents (Id, GroupId, Number, FirstName, LastName)
                    VALUES (NEWID(), @groupId, @number, @firstName, @lastName)
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
        using var conn = new SqlConnection(cs);
        conn.Open();
        EnsureTables(conn);

        new SqlCommand(@"DELETE FROM ExamGroupStudents WHERE GroupId = @groupId", conn)
            .Also(c => c.Parameters.AddWithValue("@groupId", groupId))
            .ExecuteNonQuery();

        new SqlCommand(@"DELETE FROM ExamGroups WHERE Id = @groupId AND ExamId = @examId", conn)
            .Also(c => {
                c.Parameters.AddWithValue("@groupId", groupId);
                c.Parameters.AddWithValue("@examId", examId);
            })
            .ExecuteNonQuery();

        return Ok();
    }
}

internal static class SqlCommandExtensions
{
    public static SqlCommand Also(this SqlCommand cmd, Action<SqlCommand> configure)
    {
        configure(cmd);
        return cmd;
    }
}
