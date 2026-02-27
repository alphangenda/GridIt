namespace Web.Dtos;

public class StudentDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}
