namespace Inventory_Management.Domain.Constants;

public abstract class Roles
{
    public const string Administrator = nameof(Administrator);
    public const string Manager = nameof(Manager);
    public const string Staff = nameof(Staff);

    public static readonly string[] All = { Administrator, Manager, Staff };
}