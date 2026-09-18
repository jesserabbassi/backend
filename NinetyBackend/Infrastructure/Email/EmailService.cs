using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NinetyBackend.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string email, string code)
    {
        var subject = "Ninety Gaming House - Verification Code";
        var body = $"Your verification code is: <strong>{code}</strong>. It expires in 10 minutes.";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var host = _configuration["SMTP_HOST"] ?? _configuration["Smtp:Host"];
        var portStr = _configuration["SMTP_PORT"] ?? _configuration["Smtp:Port"];
        var username = _configuration["SMTP_USERNAME"] ?? _configuration["Smtp:Username"];
        var password = _configuration["SMTP_PASSWORD"] ?? _configuration["Smtp:Password"];
        var fromAddress = _configuration["SMTP_FROM"] ?? _configuration["Smtp:From"];

        if (string.IsNullOrEmpty(fromAddress))
        {
            fromAddress = "noreply@ninetygaming.com";
        }

        if (string.IsNullOrEmpty(host))
        {
            _logger.LogWarning("SMTP host not configured. Skipping email send to {To}", to);
            return;
        }

        int port = int.TryParse(portStr, out var p) ? p : 587;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(fromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.Auto);
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password);
            }
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }
}
