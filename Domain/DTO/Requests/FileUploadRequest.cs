namespace Domain.DTO.Requests;

public class FileUploadRequest
{
    public required Stream Content { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long Length { get; set; }
}