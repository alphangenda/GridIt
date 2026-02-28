using Domain.Common;

namespace Domain.Entities.Classes;

public class Student : AuditableAndSoftDeletableEntity
{
    public Guid ClassId { get; private set; }
    public string Number { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;

    public void SetClassId(Guid classId) => ClassId = classId;
    public void SetNumber(string number) => Number = number;
    public void SetFirstName(string firstName) => FirstName = firstName;
    public void SetLastName(string lastName) => LastName = lastName;
}
