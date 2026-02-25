namespace Web.Features.Members.Sessions;

public class SessionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public List<Guid> ClassIds { get; set; } = new();
}
