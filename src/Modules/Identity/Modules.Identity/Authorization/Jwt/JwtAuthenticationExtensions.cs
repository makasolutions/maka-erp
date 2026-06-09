using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Authorization.Jwt;

internal static class JwtAuthenticationExtensions
{
    internal static IServiceCollection ConfigureJwtAuth(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(nameof(JwtOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
        services
            .AddAuthentication(authentication =>
            {
                authentication.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authentication.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, null!);

        services.AddAuthorizationBuilder().AddRequiredPermissionPolicy();
        services.AddAuthorization(options =>
        {
            // Permission evaluation lives in the RequiredPermission policy (it reads each
            // endpoint's RequiredPermissionAttribute metadata). Wire it as BOTH the default
            // AND the fallback policy (upstream fix, §18.4 #14):
            //   - FallbackPolicy covers endpoints with no auth metadata at all.
            //   - DefaultPolicy covers endpoints that opt in via .RequireAuthorization() —
            //     without this, a group-level .RequireAuthorization() applies the built-in
            //     authenticated-only default, which SUPPRESSES the fallback so
            //     .RequirePermission(...) is never evaluated (broken access control, fail-open).
            options.DefaultPolicy = options.GetPolicy(RequiredPermissionDefaults.PolicyName)!;
            options.FallbackPolicy = options.GetPolicy(RequiredPermissionDefaults.PolicyName);
        });
        return services;
    }
}