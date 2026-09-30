using Domain.Enums;

namespace Domain.DTO.Params;

public class ApplicationFilterParams
{
    public ApplicationStatus? Status { get; set; }
    public Guid? FacultyId { get; set; }
    public int? RenewalYear { get; set; }
    public string? SearchTerm  { get; set; }
}