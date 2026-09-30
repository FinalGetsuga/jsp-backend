namespace Domain.DTO;

public class ApplicationDetailDto : ApplicationSummaryDto
{
    public string? RejectionReason { get; set; }
    public virtual List<ApplicationDocumentDto> Documents { get; set; } = new();
}