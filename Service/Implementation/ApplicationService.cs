using System.Linq.Expressions;
using System.Security.Cryptography;
using Domain.DTO;
using Domain.DTO.Params;
using Domain.DTO.Requests;
using Domain.DTO.Result;
using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Interface;
using Service.Interface;

namespace Service.Implementation;

public class ApplicationService : IApplicationService
{
    private readonly IRepository<Application> _applicationRepository;
    private readonly IRepository<ApplicationDocument> _documentRepository;
    private readonly IFileStorageService _fileStorageService;

    private static readonly HashSet<string> AllowedContentTypes = new()
    {
        "image/jpeg", "image/png", "application/pdf"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    
    public ApplicationService(IRepository<Application> applicationRepository, IRepository<ApplicationDocument> documentRepository, IFileStorageService fileStorageService)
    {
        _applicationRepository = applicationRepository;
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Guid> CreateApplicationAsync(string userId, CreateApplicationRequest request, CancellationToken ct = default)
    {
        var alreadyExists =
            await _applicationRepository.ExistsAsync(x => x.UserId == userId && x.RenewalYear == request.RenewalYear);
        
        if (alreadyExists) throw new InvalidOperationException($"An application for {request.RenewalYear} already exists");

        var files = new (DocumentType Type, FileUploadRequest File)[]
        {
            (DocumentType.StudentCertificate, request.StudentCertificate),
            (DocumentType.IdCard, request.IdCard),
            (DocumentType.StudentIndex, request.StudentIndex)
        };

        foreach (var (_, file) in files)
            ValidateFile(file);
        
        var applicationId = Guid.NewGuid();
        var uploadedKeys = new List<string>();

        try
        {
            var documents = new List<ApplicationDocument>();

            foreach (var (type, file) in files)
            {
                var checkSum = ComputeChecksum(file.Content);
                var extension = Path.GetExtension(file.FileName);
                var key = $"applications/{userId}/{request.RenewalYear}/{type}{extension}";

                await _fileStorageService.UploadAsync(file.Content, key, file.ContentType, ct);
                uploadedKeys.Add(key);

                documents.Add(new ApplicationDocument
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = applicationId,
                    Type = type,
                    StorageKey = key,
                    ContentType = file.ContentType,
                    SizeBytes = file.Length,
                    Checksum = checkSum,
                    UploadedAt = DateTime.UtcNow
                });
            }

            var application = new Application
            {
                Id = applicationId,
                UserId = userId,
                RenewalYear = request.RenewalYear,
                Status = ApplicationStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                Documents = documents
            };

            await _applicationRepository.InsertAsync(application);
            return applicationId;
        }
        catch
        {
            foreach (var key in uploadedKeys)
            {
                try
                {
                    await _fileStorageService.DeleteAsync(key, ct);
                }
                catch
                {
                    
                }
            }

            throw;
        }
    }

    public async Task<ApplicationDetailDto> GetByIdAsync(Guid applicationId, string requestingUserId, CancellationToken ct = default)
    {
        var application = await _applicationRepository.GetAsync(
            selector: x => x,
            predicate: x => x.Id == applicationId,
            include: x => x.Include(y => y.User).ThenInclude(z => z.Faculty)
                .Include(y => y.Documents),
            asNoTracking: true);

        if (application == null)
        {
            throw new KeyNotFoundException("Application not found.");
        }

        if (application.UserId != requestingUserId)
        {
            throw new UnauthorizedAccessException("You do not have access to this application.");
        }

        return await MapToDetailDtoAsync(application, ct);
    }

    public async Task<PaginatedResult<ApplicationSummaryDto>> GetMyApplicationsAsync(string userId, PaginationParams pagination, CancellationToken ct = default)
    {
        return await _applicationRepository.GetAllPagedAsync(
            selector: x => new ApplicationSummaryDto
            {
                Id = x.Id,
                RenewalYear = x.RenewalYear,
                Status = x.Status,
                SubmittedAt = x.SubmittedAt,
                StudentFullName = x.User.FirstName + " " + x.User.LastName,
                FacultyName = x.User.Faculty != null ? x.User.Faculty.Name : "N/A"
            },
            pageNumber: pagination.PageNumber,
            pageSize: pagination.PageSize,
            predicate: x => x.UserId == userId,
            orderBy: y => y.OrderByDescending(x => x.SubmittedAt),
            asNoTracking: true);
    }

    public async Task ReplaceDocumentAsync(Guid applicationId, string userId, DocumentType type, FileUploadRequest file,
        CancellationToken ct = default)
    {
        ValidateFile(file);

        var application = await _applicationRepository.GetAsync(
            selector: x => x,
            predicate: x => x.Id == applicationId,
            include: x => x.Include(y => y.Documents));

        if (application == null)
        {
            throw new KeyNotFoundException("Application not found.");
        }

        if (application.UserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have access to this application.");
        }

        if (application.Status == ApplicationStatus.Approved)
        {
            throw new InvalidOperationException("Cannot modify documents on an approved application.");
        }

        var document = application.Documents.FirstOrDefault(d => d.Type == type);
        if (document == null)
        {
            throw new KeyNotFoundException($"No document of type {type} exists on this application.");
        }

        var oldKey = document.StorageKey;
        var checkSum = ComputeChecksum(file.Content);
        var extension = Path.GetExtension(file.FileName);
        var newKey = $"applications/{userId}/{application.RenewalYear}/{type}{extension}";

        await _fileStorageService.UploadAsync(file.Content, newKey, file.ContentType, ct);

        document.StorageKey = newKey;
        document.ContentType = file.ContentType;
        document.SizeBytes = file.Length;
        document.Checksum = checkSum;
        document.UploadedAt = DateTime.UtcNow;

        if (application.Status == ApplicationStatus.Rejected)
        {
            application.Status = ApplicationStatus.Pending;
            application.RejectionReason = null;
        }

        await _documentRepository.UpdateAsync(document);
        await _applicationRepository.UpdateAsync(application);
        
        if (oldKey != newKey)
        {
            try
            {
                await _fileStorageService.DeleteAsync(oldKey, ct);
            }
            catch
            {
                
            }
        }
    }

    public async Task WithdrawApplicationAsync(Guid applicationId, string userId, CancellationToken ct = default)
    {
        var application = await _applicationRepository.GetAsync(
            selector: x => x,
            predicate: x => x.Id == applicationId,
            include: x => x.Include(y => y.Documents));

        if (application == null)
        {
            throw new KeyNotFoundException("Application not found.");
        }

        if (application.UserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have access to this application.");
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending applications can be withdrawn.");
        }
        
        await _applicationRepository.DeleteAsync(application);
        
        foreach (var doc in application.Documents)
        {
            try
            {
                await _fileStorageService.DeleteAsync(doc.StorageKey, ct);
            }
            catch
            {
                
            }
        }
    }

    public async Task<PaginatedResult<ApplicationSummaryDto>> GetAllApplicationsAsync(ApplicationFilterParams filter, PaginationParams pagination,
        CancellationToken ct = default)
    {
        Expression<Func<Application, bool>> predicate = x =>
            (!filter.Status.HasValue || x.Status == filter.Status.Value) &&
            (!filter.FacultyId.HasValue || x.User.FacultyId == filter.FacultyId.Value) &&
            (!filter.RenewalYear.HasValue || x.RenewalYear == filter.RenewalYear.Value) &&
            (string.IsNullOrEmpty(filter.SearchTerm) ||
             x.User.FirstName.Contains(filter.SearchTerm) ||
             x.User.LastName.Contains(filter.SearchTerm) ||
             (x.User.StudentIndexNumber != null && x.User.StudentIndexNumber.Contains(filter.SearchTerm)));
        
        return await _applicationRepository.GetAllPagedAsync(
            selector: x => new ApplicationSummaryDto
            {
                Id = x.Id,
                RenewalYear = x.RenewalYear,
                Status = x.Status,
                SubmittedAt = x.SubmittedAt,
                StudentFullName = x.User.FirstName + " " + x.User.LastName,
                FacultyName = x.User.Faculty != null ? x.User.Faculty.Name : "N/A"
            },
            pageNumber: pagination.PageNumber,
            pageSize: pagination.PageSize,
            predicate: predicate,
            orderBy: y => y.OrderBy(x => x.Status).ThenByDescending(x => x.SubmittedAt),
            asNoTracking: true);
    }

    public async Task<ApplicationDetailDto> GetByIdForAdminAsync(Guid applicationId, CancellationToken ct = default)
    {
        var application = await _applicationRepository.GetAsync(
            selector: x => x,
            predicate: x => x.Id == applicationId,
            include: x => x.Include(y => y.User).ThenInclude(z => z.Faculty)
                .Include(y => y.Documents),
            asNoTracking: true);

        if (application == null)
        {
            throw new KeyNotFoundException("Application not found.");
        }

        return await MapToDetailDtoAsync(application, ct);
    }

    public async Task ApproveApplicationAsync(Guid applicationId, string adminId, CancellationToken ct = default)
    {
        var application = await _applicationRepository.GetAsync(
            selector: x => x,
            predicate: x => x.Id == applicationId);
        
        if (application == null)
        {
            throw new KeyNotFoundException("Application not found.");
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending applications can be approved.");
        }
        
        application.Status = ApplicationStatus.Approved;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewdByAdminId = adminId;
        application.RejectionReason = null;
        
        await _applicationRepository.UpdateAsync(application);
    }

    public async Task RejectApplicationAsync(Guid applicationId, string adminId, string rejectionReason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new ArgumentException("A rejection reason is required.");
        }
        
        var application = await _applicationRepository.GetAsync(
            selector: x => x,
            predicate: x => x.Id == applicationId);
        
        if (application == null)
        {
            throw new KeyNotFoundException("Application not found.");
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending applications can be rejected.");
        }
        
        application.Status = ApplicationStatus.Rejected;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewdByAdminId = adminId;
        application.RejectionReason = rejectionReason;
        
        await _applicationRepository.UpdateAsync(application);
    }

    private async Task<ApplicationDetailDto> MapToDetailDtoAsync(Application application,
        CancellationToken ct = default)
    {
        var documentDtos = new List<ApplicationDocumentDto>();

        foreach (var doc in application.Documents)
        {
            var url = await _fileStorageService.GetPresignedUrlAsync(doc.StorageKey, TimeSpan.FromMinutes(15), ct);
            documentDtos.Add(new ApplicationDocumentDto
            {
                Id = doc.Id,
                Type = doc.Type,
                PresignedUrl = url,
                ContentType = doc.ContentType,
                UploadedAt = doc.UploadedAt
            });
        }

        return new ApplicationDetailDto
        {
            Id = application.Id,
            RenewalYear = application.RenewalYear,
            Status = application.Status,
            SubmittedAt = application.SubmittedAt,
            StudentFullName = $"{application.User.FirstName} {application.User.LastName}",
            FacultyName = application.User.Faculty?.Name ?? "N/A",
            RejectionReason = application.RejectionReason,
            Documents = documentDtos
        };
    }
    
    private static string ComputeChecksum(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        stream.Position = 0;
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void ValidateFile(FileUploadRequest file)
    {
        if (file.Length == 0)
            throw new ArgumentException($"{file.FileName} is empty.");

        if (file.Length > MaxFileSizeBytes)
            throw new ArgumentException($"{file.FileName} exceeds the 10MB size limit.");
        
        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new ArgumentException($"{file.FileName} has an unsupported type ({file.ContentType}). Only JPEG, PNG, and PDF are allowed.");
    }
}