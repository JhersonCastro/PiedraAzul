using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using PiedraAzul.Client.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PiedraAzul.Client.States
{
    public class JwtAuthStateProvider(IJSRuntime JS, ITokenService tokenService) : AuthenticationStateProvider
    {
        private static SemaphoreSlim _refreshLock = new(1, 1);

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await tokenService.GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                //Try refreshing the token
                var refreshResult = await tokenService.RefreshTokenAsync();
                if (refreshResult == null)
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

                token = refreshResult;
            }

            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            var jwt = handler.ReadJwtToken(token);

            var claims = new List<Claim>();

            foreach (var claim in jwt.Claims)
            {
                if (claim.Type == "role" ||
                    claim.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                {
                    claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                }
                else
                {
                    claims.Add(claim);
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }

        public void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}