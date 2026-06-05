using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using PiedraAzul.Application.Common.Interfaces;
using System.Security.Claims;
// Alias: el PasskeyDto de Contracts (Id string, expuesto por GraphQL) coexiste con el
// PasskeyDto de Application (Id Guid, contrato interno del servicio).
using PasskeyDto = PiedraAzul.Contracts.DTOs.PasskeyDto;

namespace PiedraAzul.GraphQL;

public partial class Query
{
    [Authorize]
    public async Task<List<PasskeyDto>> GetMyPasskeysAsync(
        [Service] IPasskeyService passkeys,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var list = await passkeys.GetUserPasskeysAsync(userId);

        return list.Select(p => new PasskeyDto
        {
            Id = p.Id.ToString(),
            FriendlyName = p.FriendlyName,
            CreatedAt = p.CreatedAt
        }).ToList();
    }
}
