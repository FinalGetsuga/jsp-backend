using Domain.Models;

namespace Service.Interface;

public interface IFacultyService
{
    Task<List<Faculty>> GetAllAsync(CancellationToken ct = default);
    Task<Faculty> GetByIdAsync(Guid id, CancellationToken ct = default);
}