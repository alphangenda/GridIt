using Domain.Common;

namespace Domain.Entities.Sessions;

public class SessionClass : Entity
{
    public Guid SessionId { get; private set; }
    public Guid ClassId { get; private set; }

    public void SetSessionId(Guid sessionId) => SessionId = sessionId;
    public void SetClassId(Guid classId) => ClassId = classId;
}
