using Mediator;
using PiedraAzul.Application.Common.Models.Auth;

namespace PiedraAzul.Application.Features.Auth.Commands.Login
{
    public record LoginCommand(string Field, string Password) : IRequest<LoginResult>;
}