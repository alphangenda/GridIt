using Domain.Common;

namespace Domain.Entities.Classes;

public class Skill : Entity
{
    public string Label { get; private set; } = null!;

    public void SetLabel(string label) => Label = label;
}
