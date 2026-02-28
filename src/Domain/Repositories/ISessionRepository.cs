using Domain.Entities.Sessions;

namespace Domain.Repositories;

public interface ISessionRepository
{
    List<Session> GetAll();
    List<Session> GetAllByUser(string createdBy);
    Session FindById(Guid id);
    Task CreateSession(Session session);
    Task UpdateSession(Session session);
    Task DeleteSessionWithId(Guid id);
}
