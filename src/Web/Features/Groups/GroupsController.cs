using Domain.Entities.Classes;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Persistence;

namespace Web.Features.Groups;

[ApiController]
[Route("api/groups")]
public class GroupsController : ControllerBase
{
    private readonly GarneauTemplateDbContext _db;
    private readonly IConfiguration _config;

    public GroupsController(GarneauTemplateDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private static void EnsureGroupStudentsTable(NpgsqlConnection conn)
    {
        using var cmd = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS group_students (
                id         UUID DEFAULT gen_random_uuid() PRIMARY KEY,
                group_id   UUID NOT NULL,
                number     VARCHAR(100) NOT NULL,
                first_name VARCHAR(255) NOT NULL,
                last_name  VARCHAR(255) NOT NULL
            );
        ", conn);
        cmd.ExecuteNonQuery();
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var groups = _db.Groups
            .Select(g => new
            {
                g.Id,
                g.Name,
                Classes = _db.GroupClasses
                    .Where(gc => gc.GroupId == g.Id)
                    .Join(_db.Classes, gc => gc.ClassId, c => c.Id,
                        (gc, c) => new { c.Id, c.Name })
                    .ToList()
            })
            .ToList();

        return Ok(groups);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Name is required.");

        if (req.Students == null || req.Students.Count == 0)
            return BadRequest("At least one student is required to create a group.");

        var trimmedName = req.Name.Trim();
        var nameExists = _db.Groups.Any(g => g.Name.ToLower() == trimmedName.ToLower());
        if (nameExists)
            return Conflict("A group with this name already exists.");

        var group = new Group();
        group.SetId(Guid.NewGuid());
        group.SetName(trimmedName);

        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var cs = _config.GetConnectionString("DefaultConnection");
        using var conn = new NpgsqlConnection(cs);
        conn.Open();
        EnsureGroupStudentsTable(conn);

        foreach (var s in req.Students!)
        {
            using var insert = new NpgsqlCommand(@"
                INSERT INTO group_students (id, group_id, number, first_name, last_name)
                VALUES (gen_random_uuid(), @groupId, @number, @firstName, @lastName)
            ", conn);
            insert.Parameters.AddWithValue("@groupId", group.Id);
            insert.Parameters.AddWithValue("@number", s.Number);
            insert.Parameters.AddWithValue("@firstName", s.FirstName);
            insert.Parameters.AddWithValue("@lastName", s.LastName);
            insert.ExecuteNonQuery();
        }

        return Ok(new { group.Id, group.Name });
    }

    [HttpGet("{groupId}/students")]
    public IActionResult GetStudents(Guid groupId)
    {
        var cs = _config.GetConnectionString("DefaultConnection");
        using var conn = new NpgsqlConnection(cs);
        conn.Open();
        EnsureGroupStudentsTable(conn);

        var students = new List<object>();
        using var cmd = new NpgsqlCommand(@"
            SELECT id, number, first_name, last_name
            FROM group_students
            WHERE group_id = @groupId
            ORDER BY last_name, first_name
        ", conn);
        cmd.Parameters.AddWithValue("@groupId", groupId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            students.Add(new
            {
                id = reader.GetGuid(0),
                number = reader.GetString(1),
                firstName = reader.GetString(2),
                lastName = reader.GetString(3),
            });
        }

        return Ok(students);
    }

    [HttpPost("{groupId}/students")]
    public async Task<IActionResult> AddStudent(Guid groupId, [FromBody] GroupStudentEntry req)
    {
        if (string.IsNullOrWhiteSpace(req.Number) || string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
            return BadRequest("All fields are required.");

        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return NotFound();

        var cs = _config.GetConnectionString("DefaultConnection");
        using var conn = new NpgsqlConnection(cs);
        conn.Open();
        EnsureGroupStudentsTable(conn);

        var newId = Guid.NewGuid();
        using var insert = new NpgsqlCommand(@"
            INSERT INTO group_students (id, group_id, number, first_name, last_name)
            VALUES (@id, @groupId, @number, @firstName, @lastName)
        ", conn);
        insert.Parameters.AddWithValue("@id", newId);
        insert.Parameters.AddWithValue("@groupId", groupId);
        insert.Parameters.AddWithValue("@number", req.Number.Trim());
        insert.Parameters.AddWithValue("@firstName", req.FirstName.Trim());
        insert.Parameters.AddWithValue("@lastName", req.LastName.Trim());
        insert.ExecuteNonQuery();

        return Ok(new { id = newId, number = req.Number.Trim(), firstName = req.FirstName.Trim(), lastName = req.LastName.Trim() });
    }

    [HttpDelete("{groupId}/students/{studentId}")]
    public async Task<IActionResult> DeleteStudent(Guid groupId, Guid studentId)
    {
        var cs = _config.GetConnectionString("DefaultConnection");
        using var conn = new NpgsqlConnection(cs);
        conn.Open();
        EnsureGroupStudentsTable(conn);

        using var cmd = new NpgsqlCommand(@"
            DELETE FROM group_students WHERE id = @studentId AND group_id = @groupId
        ", conn);
        cmd.Parameters.AddWithValue("@studentId", studentId);
        cmd.Parameters.AddWithValue("@groupId", groupId);
        var affected = cmd.ExecuteNonQuery();

        if (affected == 0) return NotFound();
        return Ok();
    }

    [HttpDelete("{groupId}")]
    public async Task<IActionResult> Delete(Guid groupId)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return NotFound();

        var links = _db.GroupClasses.Where(gc => gc.GroupId == groupId);
        _db.GroupClasses.RemoveRange(links);
        _db.Groups.Remove(group);
        await _db.SaveChangesAsync();

        return Ok();
    }
}

public record GroupStudentEntry(string Number, string FirstName, string LastName);
public record CreateGroupRequest(string Name, List<GroupStudentEntry>? Students);
