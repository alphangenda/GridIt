using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

        using var conn = new SqlConnection(connectionString);
        conn.Open();

        var cmd = new SqlCommand("SELECT Id, Label FROM Skills", conn);
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
}