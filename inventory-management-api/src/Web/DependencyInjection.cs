using System.Security.Claims;
using Azure.Identity;
using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Infrastructure.Data;
using Inventory_Management.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Users;
using Microsoft.Extensions.DependencyInjection.Users.Commands.SyncCurrentUser;


namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddScoped<IUser, CurrentUser>();
        services.AddScoped<ICurrentUserAccess, CurrentUserAccess>();

        services.AddHttpContextAccessor();

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();

        services.AddExceptionHandler<CustomExceptionHandler>();
        
        //need this to use Controllers
        services.AddControllers();
        
        // Customise default API behaviour
        services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        services.AddEndpointsApiExplorer();

        services.AddOpenApiDocument((configure, sp) =>
        {
            configure.Title = "Inventory_Management API";

        });

        return services;
    }

    public static IServiceCollection AddAuth0(this IServiceCollection services, IConfiguration configuration)
    {
        var domain = configuration["Auth0:Domain"];
        var audience = configuration["Auth0:Audience"];
        Guard.Against.NullOrEmpty(domain, message: "Configuration 'Auth0:Domain' not found.");
        Guard.Against.NullOrEmpty(audience, message: "Configuration 'Auth0:Audience' not found.");

        // Replaces Identity's default scheme, so [Authorize(Roles = ...)] accepts both Auth0 and Identity logins.
        services.AddAuthentication(Auth0Defaults.Auth0OrIdentityScheme)
            .AddJwtBearer(Auth0Defaults.AuthenticationScheme, options =>
            {
                options.Authority = $"https://{domain}/";
                options.Audience = audience;
                options.TokenValidationParameters.RoleClaimType = Auth0Defaults.RoleClaimType;
                options.Events = new JwtBearerEvents { OnTokenValidated = ApplyRoleFromDatabase };
            })
            .AddPolicyScheme(Auth0Defaults.Auth0OrIdentityScheme, null, options =>
                options.ForwardDefaultSelector = context =>
                {
                    // Auth0 access tokens are JWTs (three dot-separated parts); Identity bearer tokens are opaque.
                    var header = context.Request.Headers.Authorization.ToString();
                    return header.StartsWith("Bearer ") && header.Count(c => c == '.') == 2
                        ? Auth0Defaults.AuthenticationScheme
                        : IdentityConstants.BearerScheme;
                });

        // Any Auth0 login, with or without a role: for endpoints a new user needs, like /api/users/me.
        services.AddAuthorization(options =>
            options.AddPolicy(Auth0Defaults.UserPolicy, policy => policy
                .AddAuthenticationSchemes(Auth0Defaults.AuthenticationScheme)
                .RequireAuthenticatedUser()));

        services.Configure<UsersOptions>(configuration.GetSection(UsersOptions.SectionName));

        return services;
    }

    // Auth0 only says who the caller is. Their role and warehouses come from our Users table,
    // so a change on the Users page applies to the next request, without logging in again.
    private static async Task ApplyRoleFromDatabase(TokenValidatedContext context)
    {
        var identity = (ClaimsIdentity)context.Principal!.Identity!;

        // JwtBearer maps "sub" to NameIdentifier.
        var auth0UserId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(auth0UserId))
        {
            context.Fail("The access token has no sub claim.");
            return;
        }

        var sender = context.HttpContext.RequestServices.GetRequiredService<ISender>();
        var user = await sender.Send(new SyncCurrentUserCommand(
            auth0UserId,
            identity.FindFirst(Auth0Defaults.EmailClaimType)?.Value,
            identity.FindFirst(Auth0Defaults.NameClaimType)?.Value), context.HttpContext.RequestAborted);

        // Read by CurrentUserAccess for the warehouse checks in the handlers.
        context.HttpContext.Items[typeof(SyncedUser)] = user;

        // Ignore any role the token carries (e.g. from an older Auth0 Action) and use ours,
        // so [Authorize(Roles = ...)] on the controllers checks the database role.
        foreach (var claim in identity.FindAll(identity.RoleClaimType).ToList())
            identity.RemoveClaim(claim);
        if (user.Role is not null)
            identity.AddClaim(new Claim(identity.RoleClaimType, user.Role));
    }

    public static IServiceCollection AddKeyVaultIfConfigured(this IServiceCollection services, ConfigurationManager configuration)
    {
        var keyVaultUri = configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }

        return services;
    }
}
