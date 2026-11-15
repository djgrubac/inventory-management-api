using Inventory_Management.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory_Management.Infrastructure.Email;

// Used when no SMTP host is configured, so the app works before Brevo is set up.
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Email:Smtp:Host is not set, so this email was not sent.\nTo: {To}\nSubject: {Subject}\n{Body}",
            toEmail, subject, textBody);
        return Task.FromResult(false);
    }
}
