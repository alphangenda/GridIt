using Domain.Entities.Classes;

namespace Domain.Repositories;

public interface IClassRepository
{
    List<Class> GetAll();
    List<Class> GetAllByUser(string createdBy);
    Class FindById(Guid id);
    Task CreateClass(Class classEntity);
    Task UpdateClass(Class classEntity);
    Task DeleteClassWithId(Guid id);
}
