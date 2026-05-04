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
        if (dto?.Letters is null)
        {
            return BadRequest("Letters payload is required.");
        }

        var cs = _config.GetConnectionString("DefaultConnection");

        using var conn = new NpgsqlConnection(cs);
        conn.Open();
        using var tx = conn.BeginTransaction();

        foreach (var letter in dto.Letters)
        {
            var cmd = new NpgsqlCommand(@"
                INSERT INTO default_criterion_letters (letter, description, default_percent, is_enabled)
                VALUES (@letter, @description, @defaultPercent, @isEnabled)
                ON CONFLICT (letter) DO UPDATE
                SET description = EXCLUDED.description,
                    default_percent = EXCLUDED.default_percent,
                    is_enabled = EXCLUDED.is_enabled
            ", conn, tx);

            cmd.Parameters.AddWithValue("@letter", letter.Letter);
            cmd.Parameters.AddWithValue("@description", letter.Description);
            cmd.Parameters.AddWithValue("@defaultPercent", letter.DefaultPercent);
            cmd.Parameters.AddWithValue("@isEnabled", letter.IsEnabled);

            cmd.ExecuteNonQuery();
        }

        var lettersToKeep = dto.Letters.Select(x => x.Letter).ToArray();
        if (lettersToKeep.Length == 0)
        {
            var deleteAllCmd = new NpgsqlCommand("DELETE FROM default_criterion_letters", conn, tx);
            deleteAllCmd.ExecuteNonQuery();
        }
        else
        {
            var deleteRemovedCmd = new NpgsqlCommand(@"
                DELETE FROM default_criterion_letters
                WHERE letter <> ALL(@lettersToKeep)
            ", conn, tx);
            deleteRemovedCmd.Parameters.AddWithValue("@lettersToKeep", lettersToKeep);
            deleteRemovedCmd.ExecuteNonQuery();
        }

        tx.Commit();
        return Ok();
    }
}
