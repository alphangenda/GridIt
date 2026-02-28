using Domain.Common;

namespace Domain.Entities.Classes;

public class ExamSkill : Entity
{
    public Guid ExamId { get; private set; }
    public Guid SkillId { get; private set; }
    public int Position { get; private set; }

    public void SetExamId(Guid examId) => ExamId = examId;
    public void SetSkillId(Guid skillId) => SkillId = skillId;
    public void SetPosition(int position) => Position = position;
}
