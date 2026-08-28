using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Server.Web.Services;

namespace Server.Web.Auth;

public sealed class RevalidatingAdminAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    AdminCredentialService credentials)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        string? claimStamp = authenticationState.User.FindFirst("admin_stamp")?.Value;
        bool isValid = authenticationState.User.Identity?.IsAuthenticated == true &&
                       string.Equals(claimStamp, credentials.GetSecurityStamp(), StringComparison.Ordinal);
        return Task.FromResult(isValid);
    }
}
