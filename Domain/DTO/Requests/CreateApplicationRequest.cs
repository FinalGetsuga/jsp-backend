namespace Domain.DTO.Requests;

public class CreateApplicationRequest
{
    public int RenewalYear { get; set; }
    public required FileUploadRequest StudentCertificate { get; set; } = default!;
    public required FileUploadRequest IdCard { get; set; } = default!;
    public required FileUploadRequest StudentIndex { get; set; } = default!;
}