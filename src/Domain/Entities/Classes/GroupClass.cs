using Domain.Common;

namespace Domain.Entities.Classes;

public class GroupClass : Entity
{
    public Guid GroupId { get; private set; }
    public Guid ClassId { get; private set; }

    public void SetGroupId(Guid groupId) => GroupId = groupId;
    public void SetClassId(Guid classId) => ClassId = classId;
}
