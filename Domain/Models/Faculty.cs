using Domain.Base;
using Domain.Identity;

namespace Domain.Models;

public class Faculty : BaseEntity
{
    public string Name { get; set; } = default!;
    public string ShortCode { get; set; } = default!;
    public string EmailDomainSegment { get; set; } = default!;
    
    public virtual ICollection<AppUser> Students { get; set; } = new List<AppUser>();
}