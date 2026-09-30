using Domain.DTO;
using Domain.DTO.Params;
using Domain.DTO.Requests;
using Domain.DTO.Result;
using Domain.Enums;

namespace Service.Interface;

public interface IApplicationService
{
    Task<Guid> CreateApplicationAsync(string userId, CreateApplicationRequest request, CancellationToken ct = default);
    Task<ApplicationDetailDto> GetByIdAsync(Guid applicationId, string requestingUserId, CancellationToken ct = default);
    Task<PaginatedResult<ApplicationSummaryDto>> GetMyApplicationsAsync(string userId, PaginationParams pagination, CancellationToken ct = default);
    Task ReplaceDocumentAsync(Guid applicationId, string userId, DocumentType type, FileUploadRequest file, CancellationToken ct = default);
    Task WithdrawApplicationAsync(Guid applicationId, string userId, CancellationToken ct = default);
    
    Task<PaginatedResult<ApplicationSummaryDto>> GetAllApplicationsAsync(ApplicationFilterParams filter, PaginationParams pagination, CancellationToken ct = default);
    Task<ApplicationDetailDto> GetByIdForAdminAsync(Guid applicationId, CancellationToken ct = default);
    Task ApproveApplicationAsync(Guid applicationId, string adminId, CancellationToken ct = default);
    Task RejectApplicationAsync(Guid applicationId, string adminId, string rejectionReason, CancellationToken ct = default);
}