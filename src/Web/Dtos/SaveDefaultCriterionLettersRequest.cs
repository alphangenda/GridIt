namespace Web.Dtos;

public class SaveDefaultCriterionLettersRequest
{
    public List<DefaultCriterionLetterDto> Letters { get; set; } = new();
}