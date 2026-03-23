using Microsoft.JSInterop;
using PiedraAzul.Shared.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace PiedraAzul.Client.Services.AuthServices
{
    public class JwtService
    {
        private readonly IJSRuntime _js;

        public JwtService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<string?> GetTokenAsync()
        {
            return await _js.InvokeAsync<string>("session.getItem", "authToken");
        }

        public async Task<UserDto?> GetCurrentUserAsync()
        {
            var token = await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
                return null;

            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
                return null;

            var jwt = handler.ReadJwtToken(token);

            return new UserDto
            {
                Id = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "",
                Email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "",
                Name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "",
                Roles = jwt.Claims
                    .Where(c =>
                        c.Type == "role" ||
                        c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                    .Select(c => c.Value)
                    .ToList()
            };
        }
    }
}
