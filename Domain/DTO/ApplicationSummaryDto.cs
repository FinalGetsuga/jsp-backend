using Domain.Enums;

namespace Domain.DTO;

public class ApplicationSummaryDto
{
    public Guid Id { get; set; }
    public int RenewalYear { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string StudentFullName { get; set; } = default!;
    public string FacultyName { get; set; } = default!;
}