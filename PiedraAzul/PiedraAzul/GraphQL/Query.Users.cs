using HotChocolate;
using HotChocolate.Authorization;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Application.Features.Users.Queries.GetUserById;
using PiedraAzul.Application.Features.Users.Queries.GetUserRoles;
using PiedraAzul.GraphQL.Inputs;
using PiedraAzul.GraphQL.Types;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.Infrastructure.Identity;
using System.Security.Claims;

namespace PiedraAzul.GraphQL;

public partial class Query
{
    [Authorize]
    public async Task<UserType> GetCurrentUserAsync(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var user = await mediator.Send(new GetUserByIdQuery(userId))
            ?? throw new GraphQLException("Usuario no encontrado");

        var roles = await mediator.Send(new GetUserRolesQuery(userId));

        return new UserType
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Roles = roles,
            EmailConfirmed = user.EmailConfirmed
        };
    }

    [Authorize(Roles = new[] { "Admin" })]
    public async Task<UserListResultType> GetUsersAsync(
        UserFilterInput filter,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IDoctorRepository doctorRepository)
    {
        var query = userManager.Users.Where(u => !u.IsDeleted);

        if (!string.IsNullOrEmpty(filter.RoleFilter))
        {
            var usersInRole = await userManager.GetUsersInRoleAsync(filter.RoleFilter);
            var ids = usersInRole.Select(u => u.Id).ToHashSet();
            query = query.Where(u => ids.Contains(u.Id));
        }

        var now = DateTimeOffset.UtcNow;
        if (filter.StatusFilter == "Active")
            query = query.Where(u => !u.LockoutEnabled || u.LockoutEnd == null || u.LockoutEnd <= now);
        else if (filter.StatusFilter == "Inactive")
            query = query.Where(u => u.LockoutEnabled && u.LockoutEnd != null && u.LockoutEnd > now);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var s = filter.SearchText.ToLower();
            query = query.Where(u =>
                u.Name.ToLower().Contains(s) ||
                (u.Email != null && u.Email.ToLower().Contains(s)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(s)));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var result = new List<UserType>();
        foreach (var u in users)
        {
            var roles = (await userManager.GetRolesAsync(u)).ToList();
            var isActive = !u.LockoutEnabled || u.LockoutEnd == null || u.LockoutEnd <= now;

            DoctorSpecialty? doctorSpecialty = null;
            if (roles.Contains("Doctor"))
            {
                var doc = await doctorRepository.GetByIdAsync(u.Id);
                if (doc is not null)
                    doctorSpecialty = (DoctorSpecialty)doc.Specialty;
            }

            result.Add(new UserType
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email ?? "",
                Phone = u.PhoneNumber ?? "",
                IdentificationNumber = u.IdentificationNumber,
                DocumentType = (PiedraAzul.Contracts.Enums.DocumentType)(int)u.DocumentType,
                Gender = (PiedraAzul.Contracts.Enums.GenderType)(int)u.Gender,
                BirthDate = u.BirthDate,
                AvatarUrl = u.AvatarUrl,
                Roles = roles,
                DoctorType = doctorSpecialty,
                IsActive = isActive,
                CreatedAt = u.CreatedAt,
                EmailConfirmed = u.EmailConfirmed
            });
        }

        return new UserListResultType
        {
            Users = result,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalPages = (int)System.Math.Ceiling((double)totalCount / filter.PageSize)
        };
    }
}
