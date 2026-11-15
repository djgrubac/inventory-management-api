namespace Microsoft.Extensions.DependencyInjection.Users;

// The "Users" section of appsettings.
public class UsersOptions
{
    public const string SectionName = "Users";

    // Auth0 ids ("sub", e.g. "auth0|6720ab...") that become Administrator on login while
    // they have no role. Matched by id, not email, so nobody can claim it by signing up
    // with someone else's address.
    public List<string> InitialAdministrators { get; set; } = new();

    // Link to the web app, put in the access email.
    public string WebBaseUrl { get; set; } = string.Empty;
}
