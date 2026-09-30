using Domain.DTO;

namespace Service.Interface;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string key, string contentType, CancellationToken ct = default);
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<List<StorageObjectInfo>> ListAllObjectsAsync(string prefix, CancellationToken ct = default);
}