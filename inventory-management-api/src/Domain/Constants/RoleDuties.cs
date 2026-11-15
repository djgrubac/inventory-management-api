namespace Inventory_Management.Domain.Constants;

// Fixed text per role, sent in the email a user gets when their access changes.
public static class RoleDuties
{
    private static readonly Dictionary<string, string> Duties = new()
    {
        [Roles.Administrator] =
            "You manage the whole system: products, categories and warehouses, and who has access to what. " +
            "You can see and change stock and orders in every warehouse.",
        [Roles.Manager] =
            "You manage products and categories, and the warehouses you are responsible for. " +
            "You can give Staff members access to your warehouses.",
        [Roles.Staff] =
            "You handle the daily work in your warehouses: checking stock, adjusting quantities " +
            "and processing incoming and outgoing orders."
    };

    public static string For(string role) => Duties.TryGetValue(role, out var duties) ? duties : string.Empty;
}
