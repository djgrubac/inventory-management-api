namespace Inventory_Management.Application.Common.Interfaces;

public interface IEmailSender
{
    // False when no email server is configured and the email was only written to the log.
    Task<bool> SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default);
}
