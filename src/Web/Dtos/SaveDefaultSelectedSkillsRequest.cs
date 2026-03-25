namespace Web.Dtos;

public class SaveDefaultSelectedSkillsRequest
{
    public List<DefaultSelectedSkillDto> Skills { get; set; } = new();
}