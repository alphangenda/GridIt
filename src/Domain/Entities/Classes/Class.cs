using Domain.Common;

namespace Domain.Entities.Classes;

public class Class : AuditableAndSoftDeletableEntity
{
    public string Name { get; private set; } = null!;
    public Guid? ProgramId { get; private set; }
    public CourseProgram? Program { get; private set; }

    public void SetName(string name) => Name = name;
    public void SetProgramId(Guid? programId) => ProgramId = programId;
}
