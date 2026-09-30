using Domain.DTO.Requests;
using Domain.DTO.Result;

namespace Service.Interface;

public interface IAuthService
{
    Task<RegisterResult> RegisterStudentAsync(RegisterStudentRequest request, CancellationToken ct = default);
    Task<RegisterResult> RegisterAdminAsync(RegisterAdminRequest request, CancellationToken ct = default);
    Task<bool> ConfirmEmailAsync(string userId, string token, CancellationToken ct = default);
    
    Task AddUserToAdminAsync(string email, CancellationToken ct = default);
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
}