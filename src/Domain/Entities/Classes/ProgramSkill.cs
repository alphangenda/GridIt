using Domain.Common;

namespace Domain.Entities.Classes;

public class ProgramSkill : Entity
{
    public Guid ProgramId { get; private set; }
    public Guid SkillId { get; private set; }

    public void SetProgramId(Guid programId) => ProgramId = programId;
    public void SetSkillId(Guid skillId) => SkillId = skillId;
}
