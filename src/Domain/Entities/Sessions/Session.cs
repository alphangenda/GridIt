using Domain.Common;
using Domain.Entities.Classes;

namespace Domain.Entities.Sessions;

public class Session : AuditableAndSoftDeletableEntity
{
    public string Name { get; private set; } = null!;
    public ICollection<Class> Classes { get; private set; } = new List<Class>();

    public void SetName(string name) => Name = name;
    public void AddClass(Class classEntity) => Classes.Add(classEntity);
    public void RemoveClass(Class classEntity) => Classes.Remove(classEntity);
    public void ClearClasses() => Classes.Clear();
}
