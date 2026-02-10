using Domain.Entities.Classes;

namespace Domain.Repositories;

public interface IExamRepository
{
    List<Exam> GetByClassId(Guid classId);
    Exam FindById(Guid id);
    Task CreateExam(Exam exam);
    Task UpdateExam(Exam exam);
    Task DeleteExamWithId(Guid id);
}
