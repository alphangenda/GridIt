using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Web.Dtos;

namespace Web.Features.DefaultSelectedSkills;

[ApiController]
[Route("api/default-selected-skills")]
public class DefaultSelectedSkillsController : ControllerBase
{
    private readonly IConfiguration _config;

    public DefaultSelectedSkillsController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var result = new List<DefaultSelectedSkillDto>();
        var cs = _config.GetConnectionString("DefaultConnection");

        using var conn = new NpgsqlConnection(cs);
        conn.Open();

        var cmd = new NpgsqlCommand(@"
            SELECT skill_id, is_selected
            FROM default_selected_skills
        ", conn);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            result.Add(new DefaultSelectedSkillDto
            {
                SkillId = reader.GetGuid(0),
                IsSelected = reader.GetBoolean(1)
            });
        }

        return Ok(result);
    }

    [HttpPost]
    public IActionResult Save(SaveDefaultSelectedSkillsRequest dto)
    {
        var cs = _config.GetConnectionString("DefaultConnection");

        using var conn = new NpgsqlConnection(cs);
        conn.Open();

        foreach (var skill in dto.Skills)
        {
            var cmd = new NpgsqlCommand(@"
                INSERT INTO default_selected_skills (skill_id, is_selected)
                VALUES (@skillId, @isSelected)
                ON CONFLICT (skill_id) DO UPDATE SET is_selected = EXCLUDED.is_selected
            ", conn);

            cmd.Parameters.AddWithValue("@skillId", skill.SkillId);
            cmd.Parameters.AddWithValue("@isSelected", skill.IsSelected);

            cmd.ExecuteNonQuery();
        }

        return Ok();
    }
}
