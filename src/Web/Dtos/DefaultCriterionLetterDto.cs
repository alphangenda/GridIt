namespace Web.Dtos;

public class DefaultCriterionLetterDto
{
    public string Letter { get; set; } = "";
    public string Description { get; set; } = "";
    public int DefaultPercent { get; set; }
    public bool IsEnabled { get; set; }
}