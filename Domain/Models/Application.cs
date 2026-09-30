using Domain.Base;
using Domain.Enums;
using Domain.Identity;

namespace Domain.Models;

public class Application : BaseEntity
{
    public string? UserId { get; set; }
    public AppUser? User { get; set; }
    
    public int RenewalYear { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    
    public string? ReviewdByAdminId { get; set; }
    public AppUser? ReviewedByAdmin { get; set; }
    
    public string? RejectionReason { get; set; }
    
    public virtual ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();

}