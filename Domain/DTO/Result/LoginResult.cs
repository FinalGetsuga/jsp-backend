namespace Domain.DTO.Result;

public class LoginResult
{
    public bool IsSuccess { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string> Errors { get; set; } = new();
}