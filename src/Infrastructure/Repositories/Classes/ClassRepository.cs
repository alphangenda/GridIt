using Application.Exceptions.Classes;
using Domain.Entities.Classes;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Infrastructure.Repositories.Classes;

public class ClassRepository : IClassRepository
{
    private readonly GarneauTemplateDbContext _context;

    public ClassRepository(GarneauTemplateDbContext context)
    {
        _context = context;
    }

    public List<Class> GetAll()
    {
        return _context.Classes.AsNoTracking().ToList();
    }

    public List<Class> GetAllByUser(string createdBy)
    {
        return _context.Classes
            .AsNoTracking()
            .Where(x => x.CreatedBy == createdBy)
            .ToList();
    }

    public Class FindById(Guid id)
    {
        var classEntity = _context.Classes
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == id);
        if (classEntity == null)
            throw new ClassNotFoundException($"Could not find class with id {id}.");
        return classEntity;
    }

    public async Task CreateClass(Class classEntity)
    {
        _context.Classes.Add(classEntity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateClass(Class classEntity)
    {
        _context.Classes.Update(classEntity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteClassWithId(Guid id)
    {
        var classEntity = _context.Classes.FirstOrDefault(x => x.Id == id);
        if (classEntity == null)
            throw new ClassNotFoundException($"Could not find class with id {id}.");

        _context.Classes.Remove(classEntity);
        await _context.SaveChangesAsync();
    }
}
