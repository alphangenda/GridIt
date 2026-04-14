namespace Domain.Repositories;

using Domain.Dtos;

public interface IClassSkillRepository
{
    Task SaveClassSkills(Guid classId, IEnumerable<Guid> skillIds);
    Task<List<SkillDto>> GetSkillsByClassId(Guid classId);
    Task ReplaceClassSkills(Guid classId, IEnumerable<Guid> skillIds);
    Task DeleteClassSkill(Guid classId, Guid skillId);
    Task<IEnumerable<Guid>> GetSkillIdsForClass(Guid classId); // conservé pour usages existants
}
