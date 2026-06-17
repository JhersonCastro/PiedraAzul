using Mediator;
using PiedraAzul.Application.Common.Models.Auth;

namespace PiedraAzul.Application.Features.Auth.Commands.MFA;

public record VerifyMFALoginCommand(string MFAToken, string OTP) : IRequest<LoginResult>;
