using Domain.Common;

namespace Domain.Entities.Classes;

public class Group : Entity
{
    public string Name { get; private set; } = null!;
    public void SetName(string name) => Name = name;
}
