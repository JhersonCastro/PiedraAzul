using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using PiedraAzul.Contracts.Auth;
using System.Security.Claims;

namespace PiedraAzul.Client.Services.AuthServices;

internal sealed class PersistentAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> _unauthenticated =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> _authenticationStateTask = _unauthenticated;

    public PersistentAuthenticationStateProvider(PersistentComponentState state)
    {
        if (!state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) || userInfo is null)
            return;

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, userInfo.UserId),
            new(ClaimTypes.Name, userInfo.Name),
            new(ClaimTypes.Email, userInfo.Email),
            new("name", userInfo.Name),
            new("avatar_url", userInfo.AvatarUrl),
            .. userInfo.Roles.Select(r => new Claim(ClaimTypes.Role, r))
        ];

        _authenticationStateTask = Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity(claims, authenticationType: nameof(PersistentAuthenticationStateProvider)))));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => _authenticationStateTask;
}
