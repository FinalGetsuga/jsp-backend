using System.ComponentModel.DataAnnotations;

namespace Domain.Base;

public class BaseEntity
{
    [Key]
    public Guid Id { get; set; }
}