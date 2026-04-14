using Domain.Common;

namespace Domain.Entities.Classes;

public class Exam : AuditableAndSoftDeletableEntity
{
    public Guid ClassId { get; private set; }
    public Guid? GroupId { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsPublic { get; private set; }

    public void SetClassId(Guid classId) => ClassId = classId;
    public void SetGroupId(Guid? groupId) => GroupId = groupId;
    public void SetName(string name) => Name = name;
    public void SetIsPublic(bool isPublic) => IsPublic = isPublic;
}
