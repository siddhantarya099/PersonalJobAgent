using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using PersonalJobAgent.Application.Interfaces;

namespace PersonalJobAgent.Infrastructure.Notifications;

public sealed class SmtpNotificationService : INotificationService
{
    private readonly SmtpOptions _options;

    public SmtpNotificationService(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task NotifyAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            return; // Or throw, depending on desired behavior if SMTP is not configured
        }

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_options.Username));
        email.To.Add(MailboxAddress.Parse(_options.Username)); // Self-notify for now
        email.Subject = message.Title;
        email.Body = new TextPart(MimeKit.Text.TextFormat.Plain) { Text = message.Body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, MailKit.Security.SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        await client.SendAsync(email, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
