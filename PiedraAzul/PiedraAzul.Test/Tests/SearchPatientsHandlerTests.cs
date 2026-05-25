using Moq;
using PiedraAzul.Application.Features.Patients.Queries.SearchPatients;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class SearchPatientsHandlerTests
{
    private readonly Mock<IPatientRepository> _registered = new();
    private readonly Mock<IPatientGuestRepository> _guest = new();

    private readonly SearchPatientsHandler _sut;

    public SearchPatientsHandlerTests()
    {
        _sut = new SearchPatientsHandler(_registered.Object, _guest.Object);
    }

    [Fact]
    public async Task SearchPatients_ReturnsBothRegisteredAndGuests()
    {
        _registered
            .Setup(x => x.SearchAsync("juan", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RegisteredPatient("reg-1", "Juan Registered")]);
        _guest
            .Setup(x => x.SearchAsync("juan", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GuestPatient("guest-1", "Juan Guest", "300", "")]);

        var result = await _sut.Handle(new SearchPatientsQuery("juan"), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Type == "Registered");
        Assert.Contains(result, x => x.Type == "Guest");
    }

    [Fact]
    public async Task SearchPatients_LimitsResultsTo10()
    {
        var registered = Enumerable.Range(1, 7)
            .Select(i => new RegisteredPatient($"reg-{i}", $"Registered {i}"))
            .ToList();
        var guests = Enumerable.Range(1, 7)
            .Select(i => new GuestPatient($"guest-{i}", $"Guest {i}", "300", ""))
            .ToList();

        _registered.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(registered);
        _guest.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(guests);

        var result = await _sut.Handle(new SearchPatientsQuery("test"), CancellationToken.None);

        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task SearchPatients_WhenNoResults_ReturnsEmpty()
    {
        _registered
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _guest
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(new SearchPatientsQuery("nobody"), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchPatients_GuestIncludesPhone()
    {
        _registered
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _guest
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GuestPatient("g-1", "Maria", "3219876543", "")]);

        var result = await _sut.Handle(new SearchPatientsQuery("maria"), CancellationToken.None);

        var guest = Assert.Single(result);
        Assert.Equal("3219876543", guest.Phone);
    }

    [Fact]
    public async Task SearchPatients_RegisteredHasEmptyPhone()
    {
        _registered
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RegisteredPatient("r-1", "Carlos")]);
        _guest
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(new SearchPatientsQuery("carlos"), CancellationToken.None);

        var registered = Assert.Single(result);
        Assert.Equal("", registered.Phone);
    }
}
