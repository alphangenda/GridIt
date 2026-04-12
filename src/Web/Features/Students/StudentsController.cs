using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Web.Dtos;

namespace Web.Features.Students;

[ApiController]
[Route("api/classes/{classId}/students")]
public class StudentsController : ControllerBase
{
    private readonly IConfiguration _config;

    public StudentsController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public IActionResult Get(Guid classId)
    {
        var result = new List<StudentDto>();
        var cs = _config.GetConnectionString("DefaultConnection");

        using var conn = new NpgsqlConnection(cs);
        conn.Open();

        var cmd = new NpgsqlCommand(@"
            SELECT id, number, first_name, last_name
            FROM students
            WHERE class_id = @classId AND deleted IS NULL
            ORDER BY last_name, first_name
        ", conn);

        cmd.Parameters.AddWithValue("@classId", classId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new StudentDto
            {
                Id = reader.GetGuid(0),
                Number = reader.GetString(1),
                FirstName = reader.GetString(2),
                LastName = reader.GetString(3)
            });
        }

        return Ok(result);
    }
}
