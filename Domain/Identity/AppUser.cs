using Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Domain.Identity;

public class AppUser : IdentityUser
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    
    public Guid? FacultyId { get; set; }
    public Faculty? Faculty { get; set; }
    
    public string? StudentIndexNumber { get; set; }
    public int? LastRenewalReminderYear { get; set; }
}