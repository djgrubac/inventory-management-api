namespace Inventory_Management.Web.Infrastructure;

public static class Auth0Defaults
{
    // Named scheme, so it runs next to the Identity scheme instead of replacing it.
    public const string AuthenticationScheme = "Auth0";

    // Default scheme: sends Auth0 JWTs to the Auth0 scheme and every other request to Identity's bearer tokens.
    public const string Auth0OrIdentityScheme = "Auth0OrIdentity";

    public const string UserPolicy = "Auth0User";

    // Role claim the API adds from the Users table after validating an Auth0 token.
    public const string RoleClaimType = "https://inventory-management-api/roles";

    // Custom claims the Auth0 post-login Action puts on the access token.
    public const string EmailClaimType = "https://inventory-management-api/email";
    public const string NameClaimType = "https://inventory-management-api/name";
}
