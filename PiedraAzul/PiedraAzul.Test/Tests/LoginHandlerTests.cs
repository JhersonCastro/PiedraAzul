using Moq;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Auth;
using PiedraAzul.Application.Common.Models.User;
using PiedraAzul.Application.Features.Auth.Commands.Login;

namespace PiedraAzul.Test.Tests;

public class LoginHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();
    private readonly LoginHandler _sut;

    public LoginHandlerTests()
    {
        _sut = new LoginHandler(_identity.Object);
    }

    [Fact]
    public async Task Login_DelegatesToIdentityService()
    {
        var user = new UserDto("user-1", "test@test.com", "Test", "", true);
        var roles = new List<string> { "Patient" };
        var expected = new LoginResult(user, roles);

        _identity
            .Setup(x => x.Login("test@test.com", "pass"))
            .ReturnsAsync(expected);

        var result = await _sut.Handle(new LoginCommand("test@test.com", "pass"), CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Login_PassesFieldAndPasswordToIdentityService()
    {
        _identity
            .Setup(x => x.Login(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new LoginResult(null, new List<string>()));

        await _sut.Handle(new LoginCommand("user@email.com", "secret123"), CancellationToken.None);

        _identity.Verify(x => x.Login("user@email.com", "secret123"), Times.Once);
    }

    [Fact]
    public async Task Login_WhenIdentityReturnsNullUser_ReturnsResultWithNullUser()
    {
        _identity
            .Setup(x => x.Login(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new LoginResult(null, new List<string>()));

        var result = await _sut.Handle(new LoginCommand("bad@email.com", "wrong"), CancellationToken.None);

        Assert.Null(result.User);
        Assert.Empty(result.Roles);
    }
}