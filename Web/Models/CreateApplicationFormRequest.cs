namespace Web.Models;

public class CreateApplicationFormRequest
{
    public int RenewalYear { get; set; }
    public IFormFile StudentCertificate { get; set; } = default!;
    public IFormFile IdCard { get; set; } = default!;
    public IFormFile StudentIndex { get; set; } = default!;
}