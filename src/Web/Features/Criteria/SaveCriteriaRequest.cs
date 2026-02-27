namespace Web.Features.Criteria;

public class SaveCriteriaRequest
{
    public Guid ExamSkillId { get; set; }
    public List<CriterionPayload> Criteria { get; set; } = [];
}

public class CriterionPayload
{
    public string Label { get; set; } = "";
    public int TotalValue { get; set; }
    public int Position { get; set; }
    public List<CriterionWeightPayload> Weights { get; set; } = [];
}

public class CriterionWeightPayload
{
    public string Weight { get; set; } = "";
    public int Value { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
}