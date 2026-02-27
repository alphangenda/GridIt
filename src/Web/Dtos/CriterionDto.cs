namespace Web.Dtos;

public class CriterionDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = "";
    public int TotalValue { get; set; }
    public int Position { get; set; }
    public List<CriterionWeightDto> Weights { get; set; } = [];
}