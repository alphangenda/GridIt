using Application.Exceptions.Classes;
using Domain.Entities.Classes;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Infrastructure.Repositories.Classes;

public class ExamRepository : IExamRepository
{
    private readonly GarneauTemplateDbContext _context;

    public ExamRepository(GarneauTemplateDbContext context)
    {
        _context = context;
    }

    public List<Exam> GetByClassId(Guid classId)
    {
        return _context.Exams
            .AsNoTracking()
            .Where(x => x.ClassId == classId)
            .ToList();
    }

    public Exam FindById(Guid id)
    {
        var exam = _context.Exams
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == id);
        if (exam == null)
            throw new ExamNotFoundException($"Could not find exam with id {id}.");
        return exam;
    }

    public async Task CreateExam(Exam exam)
    {
        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateExam(Exam exam)
    {
        _context.Exams.Update(exam);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteExamWithId(Guid id)
    {
        var exam = _context.Exams.FirstOrDefault(x => x.Id == id);
        if (exam == null)
            throw new ExamNotFoundException($"Could not find exam with id {id}.");

        _context.Exams.Remove(exam);
        await _context.SaveChangesAsync();
    }
}
