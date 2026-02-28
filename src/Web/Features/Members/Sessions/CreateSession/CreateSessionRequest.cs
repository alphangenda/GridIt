namespace Web.Features.Members.Sessions.CreateSession;

public class CreateSessionRequest
{
    public string Name { get; set; } = null!;
    public List<Guid> ClassIds { get; set; } = new();
}
