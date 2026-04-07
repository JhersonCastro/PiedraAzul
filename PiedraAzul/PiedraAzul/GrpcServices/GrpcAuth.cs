using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Auth;
using PiedraAzul.Application.Common.Models.User;
using PiedraAzul.Application.Features.Auth.Commands.Login;
using PiedraAzul.Application.Features.Auth.Commands.Register;
using PiedraAzul.Application.Features.Users.Commands.CreateProfileForRole;
using PiedraAzul.Application.Features.Users.Queries.GetUserById;
using PiedraAzul.Application.Features.Users.Queries.GetUserRoles;
using PiedraAzul.Shared.Grpc;
using Shared.Grpc;
using System.Security.Claims;

namespace PiedraAzul.GrpcServices
{
    public class GrpcAuth(
        IMediator mediator,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refresh
    ) : AuthService.AuthServiceBase
    {
        // =========================
        // LOGIN
        // =========================
        public override async Task<AuthResponse> Login(LoginRequest request, ServerCallContext context)
        {
            var result = await mediator.Send(new LoginCommand(request.Email, request.Password));

            if (result.User is null)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Credenciales incorrectas"));

            var accessToken = await jwtTokenService.CreateTokenAsync(result.User.Id, result.Roles);
            var refreshToken = await refresh.GenerateRefreshTokenAsync(result.User.Id);

            await GrpcCookieHelper.SetRefreshTokenCookie(context, refreshToken);

            return new AuthResponse
            {
                User = MapUser(result.User, result.Roles),
                AccessToken = accessToken
            };
        }

        // =========================
        // REGISTER
        // =========================
        public override async Task<AuthResponse> Register(RegisterRequest request, ServerCallContext context)
        {
            var roles = request.Roles.ToList();

            var result = await mediator.Send(new RegisterCommand(
                new RegisterUserDto(
                    request.Email,
                    request.Name,
                    request.Phone,
                    request.IdentificationNumber
                ),
                request.Password,
                roles
            ));

            if (result.User is null)
                throw new RpcException(new Status(StatusCode.Internal, "No se pudo registrar"));

            // temporal
            foreach (var role in roles)
            {
                await mediator.Send(new CreateProfileForRoleCommand(result.User.Id, role));
            }

            var accessToken = await jwtTokenService.CreateTokenAsync(result.User.Id, roles);
            var refreshToken = await refresh.GenerateRefreshTokenAsync(result.User.Id);

            await GrpcCookieHelper.SetRefreshTokenCookie(context, refreshToken);

            return new AuthResponse
            {
                User = MapUser(result.User, roles),
                AccessToken = accessToken
            };
        }

        // =========================
        // REFRESH TOKEN 
        // =========================
        public override async Task<AuthResponse> RefreshToken(RefreshTokenRequest request, ServerCallContext context)
        {
            var refreshToken = GrpcCookieHelper.GetRefreshTokenFromCookie(context);

            if (string.IsNullOrEmpty(refreshToken))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "No hay refresh token"));

            var userId = await refresh.ValidateRefreshTokenAsync(refreshToken);

            if (userId == null)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Token inválido"));

            var user = await mediator.Send(new GetUserByIdQuery(userId));
            var roles = await mediator.Send(new GetUserRolesQuery(userId));

            var newRefreshToken = await refresh.RotateRefreshTokenAsync(refreshToken);
            var accessToken = await jwtTokenService.CreateTokenAsync(userId, roles);

            await GrpcCookieHelper.SetRefreshTokenCookie(context, newRefreshToken);

            return new AuthResponse
            {
                User = MapUser(user!, roles),
                AccessToken = accessToken
            };
        }

        // =========================
        // REVOKE TOKEN
        // =========================
        public override async Task<Shared.Grpc.Empty> RevokeToken(RevokeTokenRequest request, ServerCallContext context)
        {
            var refreshToken = GrpcCookieHelper.GetRefreshTokenFromCookie(context);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await refresh.RotateRefreshTokenAsync(refreshToken);
            }

            await GrpcCookieHelper.DeleteRefreshTokenCookie(context);

            return new Shared.Grpc.Empty();
        }

        // =========================
        // CURRENT USER
        // =========================
        public override async Task<UserResponse> GetCurrentUser(Shared.Grpc.Empty request, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var userClaims = httpContext.User;

            if (userClaims?.Identity?.IsAuthenticated != true)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "No autenticado"));

            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Token inválido"));

            var user = await mediator.Send(new GetUserByIdQuery(userId));
            var roles = await mediator.Send(new GetUserRolesQuery(userId));

            if (user == null)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Usuario no encontrado"));

            return MapUser(user, roles);
        }

        // =========================
        // MAPPER
        // =========================
        private static UserResponse MapUser(UserDto user, List<string> roles)
        {
            var response = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            };

            response.Roles.AddRange(roles);

            return response;
        }
    }
}