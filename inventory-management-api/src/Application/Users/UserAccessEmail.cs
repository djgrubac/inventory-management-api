using System.Net;
using System.Text;
using Inventory_Management.Domain.Constants;
using Inventory_Management.Domain.Enums;

namespace Microsoft.Extensions.DependencyInjection.Users;

// The email a user gets when an Administrator or Manager changes their access.
public static class UserAccessEmail
{
    public record Content(string Subject, string Html, string Text);

    public static Content Build(
        string? name,
        string? role,
        IEnumerable<(string Warehouse, WarehousePermission Permission)> warehouses,
        string webBaseUrl)
    {
        var greeting = string.IsNullOrWhiteSpace(name) ? "Hello," : $"Hello {name},";

        if (role is null)
        {
            const string removed = "Your access to Inventory Management was removed. " +
                                   "Contact your manager if you think this is a mistake.";
            return new Content(
                "Your access to Inventory Management was removed",
                $"<p>{Encode(greeting)}</p><p>{Encode(removed)}</p>",
                $"{greeting}\n\n{removed}\n");
        }

        var lines = role == Roles.Administrator
            ? new List<string> { "Every warehouse: read and write" }
            : warehouses
                .OrderBy(w => w.Warehouse)
                .Select(w => $"{w.Warehouse}: {(w.Permission == WarehousePermission.Write ? "read and write" : "read only")}")
                .ToList();
        if (lines.Count == 0)
            lines.Add("No warehouses yet. Your manager will add them.");

        var subject = $"You now have the {role} role in Inventory Management";
        var duties = RoleDuties.For(role);

        var text = new StringBuilder()
            .AppendLine(greeting).AppendLine()
            .AppendLine($"You now have the {role} role.").AppendLine()
            .AppendLine("Your duties:").AppendLine(duties).AppendLine()
            .AppendLine("Your warehouses:");
        foreach (var line in lines) text.AppendLine($"- {line}");
        if (!string.IsNullOrWhiteSpace(webBaseUrl)) text.AppendLine().AppendLine($"Open the app: {webBaseUrl}");

        var html = new StringBuilder()
            .Append($"<p>{Encode(greeting)}</p>")
            .Append($"<p>You now have the <strong>{Encode(role)}</strong> role.</p>")
            .Append($"<h3>Your duties</h3><p>{Encode(duties)}</p>")
            .Append("<h3>Your warehouses</h3><ul>");
        foreach (var line in lines) html.Append($"<li>{Encode(line)}</li>");
        html.Append("</ul>");
        if (!string.IsNullOrWhiteSpace(webBaseUrl))
            html.Append($"<p><a href=\"{Encode(webBaseUrl)}\">Open Inventory Management</a></p>");

        return new Content(subject, html.ToString(), text.ToString());
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
