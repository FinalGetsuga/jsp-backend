namespace Domain.DTO.Result;

public class RegisterResult
{
    public bool IsSuccess { get; set; }
    public List<string> Errors { get; set; } = new();
}