namespace Domain.DTO.Requests;

public class RegisterStudentRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required Guid FacultyId { get; set; }
    public string? StudentIndexNumber { get; set; }
}