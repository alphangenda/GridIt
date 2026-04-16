using Domain.Entities.Classes;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Persistence;

namespace Web.Features.Members.Classes.ClassGroups;

[ApiController]
[Route("api/classes/{classId}/groups")]
public class ClassGroupsController : ControllerBase
{
    private readonly GarneauTemplateDbContext _db;
    private readonly IConfiguration _config;

    public ClassGroupsController(GarneauTemplateDbContext db, IConfiguration config)
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
    public IActionResult GetGroupsForClass(Guid classId)
    {
        var groups = _db.GroupClasses
            .Where(gc => gc.ClassId == classId)
            .Join(_db.Groups, gc => gc.GroupId, g => g.Id,
                (gc, g) => new { g.Id, g.Name })
            .OrderBy(g => g.Name)
            .ToList();

        return Ok(groups);
    }

    [HttpPost]
    public async Task<IActionResult> AddGroupToClass(Guid classId, [FromBody] AddGroupToClassRequest req)
    {
        Guid groupId;

        if (req.GroupId.HasValue)
        {
            // Associer un groupe existant
            var exists = await _db.Groups.FindAsync(req.GroupId.Value);
            if (exists == null) return NotFound("Group not found.");

            var alreadyLinked = _db.GroupClasses
                .Any(gc => gc.GroupId == req.GroupId.Value && gc.ClassId == classId);
            if (alreadyLinked) return Conflict("Group already linked to this class.");

            groupId = req.GroupId.Value;
        }
        else
        {
            // Créer un nouveau groupe — les étudiants sont obligatoires
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest("Name is required when GroupId is not provided.");

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
            groupId = group.Id;

            await _db.SaveChangesAsync();

            // Sauvegarder les étudiants dans group_students
            var cs = _config.GetConnectionString("DefaultConnection");
            using var conn = new NpgsqlConnection(cs);
            conn.Open();
            EnsureGroupStudentsTable(conn);

            foreach (var s in req.Students)
            {
                using var insert = new NpgsqlCommand(@"
                    INSERT INTO group_students (id, group_id, number, first_name, last_name)
                    VALUES (gen_random_uuid(), @groupId, @number, @firstName, @lastName)
                ", conn);
                insert.Parameters.AddWithValue("@groupId", groupId);
                insert.Parameters.AddWithValue("@number", s.Number);
                insert.Parameters.AddWithValue("@firstName", s.FirstName);
                insert.Parameters.AddWithValue("@lastName", s.LastName);
                insert.ExecuteNonQuery();
            }
        }

        var link = new GroupClass();
        link.SetId(Guid.NewGuid());
        link.SetGroupId(groupId);
        link.SetClassId(classId);
        _db.GroupClasses.Add(link);

        await _db.SaveChangesAsync();
        return Ok(new { id = groupId });
    }

    [HttpDelete("{groupId}")]
    public async Task<IActionResult> RemoveGroupFromClass(Guid classId, Guid groupId)
    {
        var link = _db.GroupClasses
            .FirstOrDefault(gc => gc.GroupId == groupId && gc.ClassId == classId);
        if (link == null) return NotFound();

        _db.GroupClasses.Remove(link);
        await _db.SaveChangesAsync();
        return Ok();
    }
}

public record GroupStudentRequest(string Number, string FirstName, string LastName);
public record AddGroupToClassRequest(Guid? GroupId, string? Name, List<GroupStudentRequest>? Students);
