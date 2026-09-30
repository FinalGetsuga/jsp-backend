using Domain.Models;
using Repository.Interface;
using Service.Interface;

namespace Service.Implementation;

public class FacultyService : IFacultyService
{
    private readonly IRepository<Faculty> _repository;

    public FacultyService(IRepository<Faculty> repository)
    {
        _repository = repository;
    }

    public async Task<List<Faculty>> GetAllAsync(CancellationToken ct = default)
    {
        return await _repository.GetAllAsync(selector: x => x);
    }

    public async Task<Faculty> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _repository.GetAsync(
            selector: x => x,
            predicate: x => x.Id == id,
            asNoTracking: true);
    }
}