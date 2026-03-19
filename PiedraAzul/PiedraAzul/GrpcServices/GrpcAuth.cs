using Google.Protobuf.Collections;
using Grpc.Core;
using PiedraAzul.ApplicationServices.Services;
using PiedraAzul.Data;
using PiedraAzul.GrpcServices;
using PiedraAzul.Shared.Grpc;
using Shared.Grpc;

namespace PiedraAzul.GrcpServices
{
    public class GrpcAuth(
        IUserService userService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refresh
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

            // 🔥 Rotar cookie
            await GrpcCookieHelper.SetRefreshTokenCookie(context, newRefreshToken);

            var userResponse = new UserResponse
            {
                Id = user.Id,
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
            var auth = context.RequestHeaders.FirstOrDefault(h => h.Key == "authorization")?.Value;
            var jwt = auth?.StartsWith("Bearer ") == true ? auth.Substring("Bearer ".Length) : null;

            if (string.IsNullOrEmpty(jwt))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "No se proporcionó token"));

            var userId = await jwtTokenService.GetUserIdByToken(jwt);

            if (userId == null)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Token inválido"));

            var user = await userService.GetById(userId);

            if (user == null)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Usuario no encontrado"));

            var roles = await userService.GetRolesByUser(user);

            var userResponse = new UserResponse
            {
                Id = user.Id,
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

            var roles = new List<string> { request.Role };

            var user = await userService.Register(userRequest, request.Password, roles);

            if (user == null)
                throw new RpcException(new Status(StatusCode.Internal, "No se pudo registrar"));

            var accessToken = await jwtTokenService.CreateTokenAsync(user);
            var refreshToken = await refresh.GenerateRefreshTokenAsync(user.Id);

            await GrpcCookieHelper.SetRefreshTokenCookie(context, refreshToken);

            var userResponse = new UserResponse
            {
                Id = user.Id,
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