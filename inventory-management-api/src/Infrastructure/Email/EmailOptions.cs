namespace Inventory_Management.Infrastructure.Email;

// The "Email" section of appsettings. User and Password belong in user secrets.
public class EmailOptions
{
    public const string SectionName = "Email";

    // Sender address. With Brevo it must be a sender verified in the Brevo dashboard.
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = "Inventory Management";

    public SmtpOptions Smtp { get; set; } = new();

    public class SmtpOptions
    {
        // Empty means "don't send": emails are written to the log instead.
        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public string? User { get; set; }
        public string? Password { get; set; }
    }
}
