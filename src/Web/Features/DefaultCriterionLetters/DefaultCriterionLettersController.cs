using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Web.Dtos;

namespace Web.Features.DefaultCriterionLetters;

[ApiController]
[Route("api/default-criterion-letters")]
public class DefaultCriterionLettersController : ControllerBase
{
    private readonly IConfiguration _config;

    public DefaultCriterionLettersController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var result = new List<DefaultCriterionLetterDto>();
        var cs = _config.GetConnectionString("DefaultConnection");

        using var conn = new NpgsqlConnection(cs);
        conn.Open();

        var cmd = new NpgsqlCommand(@"
            SELECT letter, description, default_percent, is_enabled
            FROM default_criterion_letters
            ORDER BY letter
        ", conn);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            result.Add(new DefaultCriterionLetterDto
            {
                Letter = reader.GetString(0),
                Description = reader.GetString(1),
                DefaultPercent = reader.GetInt32(2),
                IsEnabled = reader.GetBoolean(3)
            });
        }

        return Ok(result);
    }

    [HttpPost]
    public IActionResult Save(SaveDefaultCriterionLettersRequest dto)
    {
        var cs = _config.GetConnectionString("DefaultConnection");

        using var conn = new NpgsqlConnection(cs);
        conn.Open();

        foreach (var letter in dto.Letters)
        {
            var cmd = new NpgsqlCommand(@"
                UPDATE default_criterion_letters
                SET description = @description,
                    default_percent = @defaultPercent,
                    is_enabled = @isEnabled
                WHERE letter = @letter
            ", conn);

            cmd.Parameters.AddWithValue("@letter", letter.Letter);
            cmd.Parameters.AddWithValue("@description", letter.Description);
            cmd.Parameters.AddWithValue("@defaultPercent", letter.DefaultPercent);
            cmd.Parameters.AddWithValue("@isEnabled", letter.IsEnabled);

            cmd.ExecuteNonQuery();
        }

        return Ok();
    }
}
