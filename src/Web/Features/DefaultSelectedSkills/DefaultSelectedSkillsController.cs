using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

        using var conn = new SqlConnection(cs);
        conn.Open();

        var cmd = new SqlCommand(@"
            SELECT SkillId, IsSelected
            FROM DefaultSelectedSkills
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

        using var conn = new SqlConnection(cs);
        conn.Open();

        foreach (var skill in dto.Skills)
        {
            var cmd = new SqlCommand(@"
                IF EXISTS (SELECT 1 FROM DefaultSelectedSkills WHERE SkillId = @skillId)
                    UPDATE DefaultSelectedSkills
                    SET IsSelected = @isSelected
                    WHERE SkillId = @skillId
                ELSE
                    INSERT INTO DefaultSelectedSkills (SkillId, IsSelected)
                    VALUES (@skillId, @isSelected)
            ", conn);

            cmd.Parameters.AddWithValue("@skillId", skill.SkillId);
            cmd.Parameters.AddWithValue("@isSelected", skill.IsSelected);

            cmd.ExecuteNonQuery();
        }

        return Ok();
    }
}