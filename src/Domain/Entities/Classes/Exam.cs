using Domain.Common;

namespace Domain.Entities.Classes;

public class Exam : AuditableAndSoftDeletableEntity
{
    public Guid ClassId { get; private set; }
    public string Name { get; private set; } = null!;

    public void SetClassId(Guid classId) => ClassId = classId;
    public void SetName(string name) => Name = name;
}
