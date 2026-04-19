using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PiedraAzul.Contracts.Auth;
using PiedraAzul.Infrastructure.Identity;
using System.Security.Claims;

namespace PiedraAzul.Extensions;

internal sealed class PersistingRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PersistentComponentState _state;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IdentityOptions _options;
    private readonly PersistingComponentStateSubscription _subscription;
    private Task<AuthenticationState>? _authenticationStateTask;

    public PersistingRevalidatingAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory serviceScopeFactory,
        PersistentComponentState persistentComponentState,
        IOptions<IdentityOptions> optionsAccessor,
        IHttpContextAccessor httpContextAccessor)
        : base(loggerFactory)
    {
        _scopeFactory = serviceScopeFactory;
        _state = persistentComponentState;
        _options = optionsAccessor.Value;
        _httpContextAccessor = httpContextAccessor;

        AuthenticationStateChanged += OnAuthenticationStateChanged;
        _subscription = persistentComponentState.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);
    }

    // During SSR prerender there is no Blazor Server circuit, so SetAuthenticationState() is
    // never called and the base implementation would return an unauthenticated principal.
    // We override GetAuthenticationStateAsync() to read directly from HttpContext.User when
    // an HttpContext is available (SSR), and fall back to the circuit state otherwise.
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
            return Task.FromResult(new AuthenticationState(httpContext.User));

        return base.GetAuthenticationStateAsync();
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await ValidateSecurityStampAsync(userManager, authenticationState.User);
    }

    private async Task<bool> ValidateSecurityStampAsync(UserManager<ApplicationUser> userManager, ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return false;
        if (!userManager.SupportsUserSecurityStamp) return true;

        var principalStamp = principal.FindFirstValue(_options.ClaimsIdentity.SecurityStampClaimType);
        var userStamp = await userManager.GetSecurityStampAsync(user);
        return principalStamp == userStamp;
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
        => _authenticationStateTask = task;

    private async Task OnPersistingAsync()
    {
        // Determine the principal:
        //  • SSR prerender  → HttpContext is present, use HttpContext.User directly.
        //  • Blazor Server circuit → HttpContext is null, use the circuit auth-state task.
        ClaimsPrincipal principal;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            principal = httpContext.User;
        }
        else if (_authenticationStateTask is not null)
        {
            var authState = await _authenticationStateTask;
            principal = authState.User;
        }
        else
        {
            // Neither context is available — nothing to persist.
            return;
        }

        if (principal.Identity?.IsAuthenticated == true)
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return; // No user ID — nothing useful to persist

            var email   = principal.FindFirstValue(ClaimTypes.Email) ?? "";
            var name    = principal.FindFirstValue("name") ?? principal.FindFirstValue(ClaimTypes.Name) ?? "";
            var avatarUrl = principal.FindFirstValue("avatar_url") ?? "default.png";
            var roles   = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            _state.PersistAsJson(nameof(UserInfo), new UserInfo
            {
                UserId    = userId,
                Name      = name,
                Email     = email,
                AvatarUrl = avatarUrl,
                Roles     = roles
            });
        }
    }

    protected override void Dispose(bool disposing)
    {
        _subscription.Dispose();
        AuthenticationStateChanged -= OnAuthenticationStateChanged;
        base.Dispose(disposing);
    }
}
