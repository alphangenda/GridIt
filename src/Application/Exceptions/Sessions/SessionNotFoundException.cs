namespace Application.Exceptions.Sessions;

public class SessionNotFoundException : Exception
{
    public SessionNotFoundException(string message) : base(message)
    {
    }
}
