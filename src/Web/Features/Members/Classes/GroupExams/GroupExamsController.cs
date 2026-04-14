using Domain.Entities.Classes;
using Microsoft.AspNetCore.Mvc;
using Persistence;

namespace Web.Features.Members.Classes.GroupExams;

[ApiController]
[Route("api/classes/{classId}/groups/{groupId}/exams")]
public class GroupExamsController : ControllerBase
{
    private readonly GarneauTemplateDbContext _db;

    public GroupExamsController(GarneauTemplateDbContext db) => _db = db;

    [HttpGet]
    public IActionResult GetExamsForGroup(Guid classId, Guid groupId)
    {
        var exams = _db.Exams
            .Where(e => e.ClassId == classId && e.GroupId == groupId)
            .Select(e => new { e.Id, e.Name })
            .OrderBy(e => e.Name)
            .ToList();

        return Ok(exams);
    }

    [HttpPost]
    public async Task<IActionResult> CreateExam(Guid classId, Guid groupId,
        [FromBody] CreateGroupExamRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Name is required.");

        var trimmedName = req.Name.Trim();
        var nameExists = _db.Exams.Any(e =>
            e.ClassId == classId &&
            e.GroupId == groupId &&
            e.Name.ToLower() == trimmedName.ToLower());
        if (nameExists)
            return Conflict("An evaluation with this name already exists in this group.");

        var exam = new Exam();
        exam.SetId(Guid.NewGuid());
        exam.SetClassId(classId);
        exam.SetGroupId(groupId);
        exam.SetName(trimmedName);
        exam.SetIsPublic(false);

        _db.Exams.Add(exam);
        await _db.SaveChangesAsync();

        return Ok(new { exam.Id, exam.Name });
    }

    [HttpDelete("{examId}")]
    public async Task<IActionResult> DeleteExam(Guid classId, Guid groupId, Guid examId)
    {
        var exam = _db.Exams
            .FirstOrDefault(e => e.Id == examId && e.ClassId == classId && e.GroupId == groupId);
        if (exam == null) return NotFound();

        _db.Exams.Remove(exam);
        await _db.SaveChangesAsync();
        return Ok();
    }
}

public record CreateGroupExamRequest(string Name);
