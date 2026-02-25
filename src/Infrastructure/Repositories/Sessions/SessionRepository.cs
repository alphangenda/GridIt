using Application.Exceptions.Sessions;
using Domain.Entities.Sessions;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Infrastructure.Repositories.Sessions;

public class SessionRepository : ISessionRepository
{
    private readonly GarneauTemplateDbContext _context;

    public SessionRepository(GarneauTemplateDbContext context)
    {
        _context = context;
    }

    public List<Session> GetAll()
    {
        return _context.Sessions
            .AsNoTracking()
            .Include(s => s.Classes)
            .ToList();
    }

    public List<Session> GetAllByUser(string createdBy)
    {
        return _context.Sessions
            .AsNoTracking()
            .Include(s => s.Classes)
            .Where(x => x.CreatedBy == createdBy)
            .ToList();
    }

    public Session FindById(Guid id)
    {
        var session = _context.Sessions
            .AsNoTracking()
            .Include(s => s.Classes)
            .FirstOrDefault(x => x.Id == id);
        if (session == null)
            throw new SessionNotFoundException($"Could not find session with id {id}.");
        return session;
    }

    public async Task CreateSession(Session session)
    {
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSession(Session session)
    {
        _context.Sessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSessionWithId(Guid id)
    {
        var session = _context.Sessions.FirstOrDefault(x => x.Id == id);
        if (session == null)
            throw new SessionNotFoundException($"Could not find session with id {id}.");

        _context.Sessions.Remove(session);
        await _context.SaveChangesAsync();
    }
}
