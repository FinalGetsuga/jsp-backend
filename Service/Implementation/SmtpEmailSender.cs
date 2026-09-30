using Domain.Email;
using Microsoft.Extensions.Options;
using MimeKit;
using Service.Interface;

namespace Service.Implementation;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string message, CancellationToken ct = default)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        msg.To.Add(MailboxAddress.Parse(email));
        msg.Subject = subject;
        msg.Body = new TextPart("html") { Text = message };

        using var client = new MailKit.Net.Smtp.SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, _options.UseSsl, ct);
        if (!string.IsNullOrEmpty(_options.Username))
            if (_options.Password != null)
                await client.AuthenticateAsync(_options.Username, _options.Password, ct);

        await client.SendAsync(msg, ct);
        await client.DisconnectAsync(true, ct);
    }
}