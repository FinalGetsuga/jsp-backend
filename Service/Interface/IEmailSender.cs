namespace Service.Interface;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message, CancellationToken ct = default);
}