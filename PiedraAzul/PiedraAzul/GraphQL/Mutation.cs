using HotChocolate;
using Mediator;
using Microsoft.AspNetCore.Http;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Auth;
using PiedraAzul.Application.Common.Models.Patients;
using PiedraAzul.Application.Features.Appointments.CreateAppointment;
using PiedraAzul.Application.Features.Auth.Commands.Login;
using PiedraAzul.Application.Features.Auth.Commands.Register;
using PiedraAzul.Application.Features.Users.Commands.CreateProfileForRole;
using PiedraAzul.Application.Features.Users.Queries.GetUserById;
using PiedraAzul.Application.Features.Users.Queries.GetUserRoles;
using PiedraAzul.GraphQL.Inputs;
using PiedraAzul.GraphQL.Types;

namespace PiedraAzul.GraphQL;

public class Mutation
{
    public async Task<AuthResponseType> LoginAsync(
        LoginInput input,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IJwtTokenService jwtTokenService,
        [Service] IRefreshTokenService refresh)
    {
        var result = await mediator.Send(new LoginCommand(input.Email, input.Password));

        if (result.User is null)
            throw new GraphQLException("Credenciales incorrectas");

        var accessToken = await jwtTokenService.CreateTokenAsync(result.User.Id, result.Roles);
        var refreshToken = await refresh.GenerateRefreshTokenAsync(result.User.Id);

        SetRefreshTokenCookie(httpContextAccessor.HttpContext!, refreshToken);

        return new AuthResponseType
        {
            AccessToken = accessToken,
            User = new UserType
            {
                Id = result.User.Id,
                Name = result.User.Name,
                Email = result.User.Email,
                Roles = result.Roles
            }
        };
    }

    public async Task<AuthResponseType> RegisterAsync(
        RegisterInput input,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IJwtTokenService jwtTokenService,
        [Service] IRefreshTokenService refresh)
    {
        var result = await mediator.Send(new RegisterCommand(
            new RegisterUserDto(input.Email, input.Name, input.Phone, input.IdentificationNumber),
            input.Password,
            input.Roles
        ));

        if (result.User is null)
            throw new GraphQLException("No se pudo registrar");

        foreach (var role in input.Roles)
            await mediator.Send(new CreateProfileForRoleCommand(result.User.Id, role));

        var accessToken = await jwtTokenService.CreateTokenAsync(result.User.Id, input.Roles);
        var refreshToken = await refresh.GenerateRefreshTokenAsync(result.User.Id);

        SetRefreshTokenCookie(httpContextAccessor.HttpContext!, refreshToken);

        return new AuthResponseType
        {
            AccessToken = accessToken,
            User = new UserType
            {
                Id = result.User.Id,
                Name = result.User.Name,
                Email = result.User.Email,
                Roles = input.Roles
            }
        };
    }

    public async Task<AuthResponseType> RefreshTokenAsync(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IJwtTokenService jwtTokenService,
        [Service] IRefreshTokenService refresh)
    {
        var httpContext = httpContextAccessor.HttpContext!;
        var refreshToken = httpContext.Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            throw new GraphQLException("No hay refresh token");

        var userId = await refresh.ValidateRefreshTokenAsync(refreshToken);

        if (userId == null)
            throw new GraphQLException("Token inválido");

        var user = await mediator.Send(new GetUserByIdQuery(userId));
        var roles = await mediator.Send(new GetUserRolesQuery(userId));

        var newRefreshToken = await refresh.RotateRefreshTokenAsync(refreshToken);
        var accessToken = await jwtTokenService.CreateTokenAsync(userId, roles);

        SetRefreshTokenCookie(httpContext, newRefreshToken);

        return new AuthResponseType
        {
            AccessToken = accessToken,
            User = new UserType
            {
                Id = user!.Id,
                Name = user.Name,
                Email = user.Email,
                Roles = roles
            }
        };
    }

    public async Task<bool> RevokeTokenAsync(
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IRefreshTokenService refresh)
    {
        var httpContext = httpContextAccessor.HttpContext!;
        var refreshToken = httpContext.Request.Cookies["refreshToken"];

        if (!string.IsNullOrEmpty(refreshToken))
            await refresh.RotateRefreshTokenAsync(refreshToken);

        httpContext.Response.Cookies.Delete("refreshToken");

        return true;
    }

    public async Task<AppointmentType> CreateAppointmentAsync(
        CreateAppointmentInput input,
        [Service] IMediator mediator)
    {
        if (string.IsNullOrWhiteSpace(input.DoctorId))
            throw new GraphQLException("DoctorId requerido");

        if (!Guid.TryParse(input.DoctorAvailabilitySlotId, out var slotId))
            throw new GraphQLException("SlotId inválido");

        var date = DateOnly.FromDateTime(input.Date);

        GuestPatientRequest? patientGuest = null;

        if (input.Guest is not null)
        {
            patientGuest = new GuestPatientRequest
            {
                Identification = input.Guest.Identification,
                Name = input.Guest.Name,
                Phone = input.Guest.Phone,
                ExtraInfo = input.Guest.ExtraInfo
            };
        }

        var appointment = await mediator.Send(
            new CreateAppointmentCommand(
                input.DoctorId,
                slotId,
                date,
                input.PatientUserId,
                patientGuest
            )
        );

        return AppointmentType.FromDomain(appointment);
    }

    private static void SetRefreshTokenCookie(HttpContext httpContext, string refreshToken)
    {
        httpContext.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            MaxAge = TimeSpan.FromDays(7),
            Path = "/"
        });
    }
}
