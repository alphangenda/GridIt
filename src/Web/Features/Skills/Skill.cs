namespace Web.Features.Skills;

public class Skill
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}