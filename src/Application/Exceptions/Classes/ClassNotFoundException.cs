namespace Application.Exceptions.Classes;

public class ClassNotFoundException : Exception
{
    public ClassNotFoundException(string message) : base(message) { }
}
