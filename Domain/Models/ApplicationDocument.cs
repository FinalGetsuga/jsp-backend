using Domain.Base;
using Domain.Enums;

namespace Domain.Models;

public class ApplicationDocument : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = default!;
    
    public DocumentType Type { get; set; }
    public string StorageKey { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
    public string Checksum { get; set; } = default!;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

}