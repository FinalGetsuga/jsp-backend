using Domain.Enums;

namespace Domain.DTO;

public class ApplicationDocumentDto
{
    public Guid Id { get; set; }
    public DocumentType Type { get; set; }
    public string PresignedUrl { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
}