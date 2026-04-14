using Domain.Dtos;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Web.Dtos;

namespace Web.Features.ClassSkills;

[ApiController]
[Route("api/classes/{classId:guid}/skills")]
public class ClassSkillsController : ControllerBase
{
    private readonly IClassSkillRepository _classSkillRepository;

    public ClassSkillsController(IClassSkillRepository classSkillRepository)
    {
        _classSkillRepository = classSkillRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetSkills(Guid classId)
    {
        var skills = await _classSkillRepository.GetSkillsByClassId(classId);
        return Ok(skills);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSkills(Guid classId, [FromBody] SaveClassSkillsRequest request)
    {
        await _classSkillRepository.ReplaceClassSkills(classId, request.SkillIds);
        return NoContent();
    }

    [HttpDelete("{skillId:guid}")]
    public async Task<IActionResult> DeleteSkill(Guid classId, Guid skillId)
    {
        await _classSkillRepository.DeleteClassSkill(classId, skillId);
        return NoContent();
    }
}
