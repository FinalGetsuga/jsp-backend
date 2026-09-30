namespace Domain.DTO.Requests;

public class ConfirmEmailRequest
{
    public required string UserId { get; set; }
    public required string Token { get; set; }
}