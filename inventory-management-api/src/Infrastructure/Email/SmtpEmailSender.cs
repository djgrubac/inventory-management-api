using Inventory_Management.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Inventory_Management.Infrastructure.Email;

// Sends through any SMTP server: Brevo (smtp-relay.brevo.com:587) or Mailpit (localhost:1025).
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody }.ToMessageBody();

        using var client = new SmtpClient();
        // Only registered when Host is set. Brevo upgrades to TLS on 587; Mailpit has no
        // TLS, so only use it when offered.
        await client.ConnectAsync(_options.Smtp.Host!, _options.Smtp.Port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
        if (!string.IsNullOrEmpty(_options.Smtp.User))
        {
            await client.AuthenticateAsync(_options.Smtp.User, _options.Smtp.Password ?? string.Empty, cancellationToken);
        }
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        return true;
    }
}
