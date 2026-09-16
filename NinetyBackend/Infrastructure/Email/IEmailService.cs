namespace NinetyBackend.Infrastructure.Email;

public interface IEmailService
{
    Task SendOtpEmailAsync(string email, string code);
    Task SendEmailAsync(string to, string subject, string body);
}
