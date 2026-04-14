using Domain.Dtos;
using Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Infrastructure.Repositories.Classes;

public class ClassSkillRepository : IClassSkillRepository
{
    private readonly IConfiguration _configuration;

    public ClassSkillRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SaveClassSkills(Guid classId, IEnumerable<Guid> skillIds)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var skillId in skillIds.Distinct())
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO class_skills (class_id, skill_id)
                VALUES (@ClassId, @SkillId)
                """,
                connection
            );

            command.Parameters.AddWithValue("@ClassId", classId);
            command.Parameters.AddWithValue("@SkillId", skillId);

            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<List<SkillDto>> GetSkillsByClassId(Guid classId)
    {
        var result = new List<SkillDto>();
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT s.id, s.label
            FROM class_skills cs
            INNER JOIN skills s ON s.id = cs.skill_id
            WHERE cs.class_id = @ClassId
            ORDER BY s.label
            """,
            connection
        );

        command.Parameters.AddWithValue("@ClassId", classId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new SkillDto
            {
                Id = reader.GetGuid(0),
                Label = reader.GetString(1)
            });
        }

        return result;
    }

    public async Task ReplaceClassSkills(Guid classId, IEnumerable<Guid> skillIds)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using (var deleteCommand = new NpgsqlCommand(
            "DELETE FROM class_skills WHERE class_id = @ClassId",
            connection))
        {
            deleteCommand.Parameters.AddWithValue("@ClassId", classId);
            await deleteCommand.ExecuteNonQueryAsync();
        }

        foreach (var skillId in skillIds.Distinct())
        {
            await using var insertCommand = new NpgsqlCommand(
                """
                INSERT INTO class_skills (class_id, skill_id)
                VALUES (@ClassId, @SkillId)
                """,
                connection
            );

            insertCommand.Parameters.AddWithValue("@ClassId", classId);
            insertCommand.Parameters.AddWithValue("@SkillId", skillId);

            await insertCommand.ExecuteNonQueryAsync();
        }
    }

    public async Task DeleteClassSkill(Guid classId, Guid skillId)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            DELETE FROM class_skills
            WHERE class_id = @ClassId AND skill_id = @SkillId
            """,
            connection
        );

        command.Parameters.AddWithValue("@ClassId", classId);
        command.Parameters.AddWithValue("@SkillId", skillId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Guid>> GetSkillIdsForClass(Guid classId)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var results = new List<Guid>();

        await using var command = new NpgsqlCommand(
            """
            SELECT skill_id
            FROM class_skills
            WHERE class_id = @ClassId
            ORDER BY skill_id
            """,
            connection
        );
        command.Parameters.AddWithValue("@ClassId", classId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetGuid(0));
        }

        return results;
    }
}
