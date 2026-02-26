namespace Web.Features.Skills;

public class ExamSkill
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public Guid SkillId { get; set; }
    public int Position { get; set; }
}