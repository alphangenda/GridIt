namespace Web.Dtos;

public class CriterionWeightDto
{
    public string Weight { get; set; } = "";
    public int Value { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
}