using Google.Protobuf.Collections;
using Grpc.Core;
using PiedraAzul.ApplicationServices.AutoCompleteServices;
using PiedraAzul.ApplicationServices.Services;
using PiedraAzul.Data;
using PiedraAzul.GrpcServices;
using PiedraAzul.Shared.Grpc;
using Shared.Grpc;
using System.Security.Claims;

namespace PiedraAzul.GrpcServices
{
    public class GrpcAuth(
        IUserService userService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refresh,
        IPatientAutocompleteService patientAutocompleteService
        //IDoctorAutocompleteService doctorAutoCompleteService
    ) : AuthService.AuthServiceBase
    {
        public override async Task<AuthResponse> Login(LoginRequest request, ServerCallContext context)
        {
            var userAndRoles = await userService.Login(request.Email, request.Password);
            var user = userAndRoles.Item1;
            var roles = userAndRoles.Item2;

            if (user == null)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Credenciales incorrectas"));

            var accessToken = await jwtTokenService.CreateTokenAsync(user);
            var refreshToken = await refresh.GenerateRefreshTokenAsync(user.Id);

            await GrpcCookieHelper.SetRefreshTokenCookie(context, refreshToken);

            var userResponse = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                BirthDate = user.BirthDate?.ToShortDateString() ?? string.Empty,
                Gender = (int)user.Gender,
                IdentificationNumber = user.IdentificationNumber,
            };
            userResponse.Roles.AddRange(roles);

            return new AuthResponse
            {
                User = userResponse,
                AccessToken = accessToken
            };
        }

        public override async Task<AuthResponse> RefreshToken(RefreshTokenRequest request, ServerCallContext context)
        {
            var refreshToken = GrpcCookieHelper.GetRefreshTokenFromCookie(context);

            if (string.IsNullOrEmpty(refreshToken))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "No hay refresh token"));

            var storedToken = await refresh.ValidateRefreshTokenAsync(refreshToken);

            if (storedToken == null)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Token inválido"));

            var user = storedToken.User;
            var roles = await userService.GetRolesByUser(user);

            var newRefreshToken = await refresh.RotateRefreshTokenAsync(storedToken);
            var accessToken = await jwtTokenService.CreateTokenAsync(user);

            await GrpcCookieHelper.SetRefreshTokenCookie(context, newRefreshToken);

            var userResponse = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                BirthDate = user.BirthDate?.ToShortDateString() ?? string.Empty,
                Gender = (int)user.Gender,
                IdentificationNumber = user.IdentificationNumber,
            };
            userResponse.Roles.AddRange(roles);

            return new AuthResponse
            {
                User = userResponse,
                AccessToken = accessToken
            };
        }

        public override async Task<Empty> RevokeToken(RevokeTokenRequest request, ServerCallContext context)
        {
            var refreshToken = GrpcCookieHelper.GetRefreshTokenFromCookie(context);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                var storedToken = await refresh.ValidateRefreshTokenAsync(refreshToken);
                if (storedToken != null) 
                    await refresh.RotateRefreshTokenAsync(storedToken);
            }

            await GrpcCookieHelper.DeleteRefreshTokenCookie(context);

            return new Empty();
        }

        public override async Task<UserResponse> GetCurrentUser(Empty request, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var userClaims = httpContext.User;

            if (userClaims?.Identity?.IsAuthenticated != true)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "No autenticado"));

            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Token inválido"));

            var user = await userService.GetById(userId);

            if (user == null)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Usuario no encontrado"));

            var roles = await userService.GetRolesByUser(user);

            var userResponse = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                BirthDate = user.BirthDate?.ToShortDateString() ?? string.Empty,
                Gender = (int)user.Gender,
                IdentificationNumber = user.IdentificationNumber,
            };

            userResponse.Roles.AddRange(roles);

            return userResponse;
        }

        public override async Task<AuthResponse> Register(RegisterRequest request, ServerCallContext context)
        {
            ApplicationUser userRequest = new ApplicationUser
            {
                Email = request.Email,
                IdentificationNumber = request.IdentificationNumber,
                PhoneNumber = request.Phone,
                Gender = (Shared.Enums.GenderType)request.Gender,
                BirthDate = request.BirthDate.ToDateTime(),
                Name = request.Name,
            };

            var roles = request.Roles.ToList();
            var user = await userService.Register(userRequest, request.Password, roles);

            if (user == null)
                throw new RpcException(new Status(StatusCode.Internal, "No se pudo registrar"));


            foreach (var role in roles)
            {
                await userService.CreateProfileForRoleAsync(user, role);
            }
            var accessToken = await jwtTokenService.CreateTokenAsync(user);
            var refreshToken = await refresh.GenerateRefreshTokenAsync(user.Id);

            await GrpcCookieHelper.SetRefreshTokenCookie(context, refreshToken);

            var userResponse = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                BirthDate = user.BirthDate?.ToShortDateString() ?? string.Empty,
                Gender = (int)user.Gender,
                IdentificationNumber = user.IdentificationNumber,
            };
            userResponse.Roles.AddRange(roles);

            return new AuthResponse
            {
                User = userResponse,
                AccessToken = accessToken
            };
        }
    }
}