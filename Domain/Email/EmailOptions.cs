namespace Domain.Email;

public class EmailOptions
{
    public required string Host { get; set; }
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public required string FromAddress { get; set; }
    public required string FromName { get; set; }
    public bool UseSsl { get; set; }
}