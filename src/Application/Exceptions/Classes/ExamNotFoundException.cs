namespace Application.Exceptions.Classes;

public class ExamNotFoundException : Exception
{
    public ExamNotFoundException(string message) : base(message) { }
}
