namespace Web.Dtos;

public class GroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public List<ClassSummaryDto> Classes { get; set; } = new();
}

public class ClassSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}
