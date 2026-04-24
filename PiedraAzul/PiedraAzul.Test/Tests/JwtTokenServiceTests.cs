using Moq;
using PiedraAzul.Application.Features.Patients.Commands.CreateGuestPatient;
using PiedraAzul.Application.Features.Patients.Queries.SearchPatients;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public async Task CreateGuestPatient_ReturnsGeneratedId()
    {
        var repo = new Mock<IPatientGuestRepository>();
        var sut = new CreateGuestPatientHandler(repo.Object, new ImmediateUnitOfWork());

        var result = await sut.Handle(new CreateGuestPatientCommand("id", "Nuevo", "300", ""), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public async Task CreateGuestPatient_AddsGuestToRepository()
    {
        var repo = new Mock<IPatientGuestRepository>();
        var sut = new CreateGuestPatientHandler(repo.Object, new ImmediateUnitOfWork());

        await sut.Handle(new CreateGuestPatientCommand("id", "Nuevo", "300", ""), CancellationToken.None);

        repo.Verify(x => x.AddAsync(It.IsAny<GuestPatient>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchPatients_CombinesRegisteredAndGuests()
    {
        var registeredRepo = new Mock<IPatientRepository>();
        var guestRepo = new Mock<IPatientGuestRepository>();

        registeredRepo.Setup(x => x.SearchAsync("ana", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RegisteredPatient("u-1", "Ana Reg")]);
        guestRepo.Setup(x => x.SearchAsync("ana", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GuestPatient("g-1", "Ana Guest", "300", "")]);

        var sut = new SearchPatientsHandler(registeredRepo.Object, guestRepo.Object);

        var result = await sut.Handle(new SearchPatientsQuery("ana"), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Type == "Registered");
        Assert.Contains(result, x => x.Type == "Guest");
    }

    [Fact]
    public async Task SearchPatients_LimitsToTenResults()
    {
        var registeredRepo = new Mock<IPatientRepository>();
        var guestRepo = new Mock<IPatientGuestRepository>();

        registeredRepo.Setup(x => x.SearchAsync("all", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(1, 7).Select(i => new RegisteredPatient($"u-{i}", $"Reg {i}")).ToList());
        guestRepo.Setup(x => x.SearchAsync("all", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(1, 7).Select(i => new GuestPatient($"g-{i}", $"Guest {i}", "300", "")).ToList());

        var sut = new SearchPatientsHandler(registeredRepo.Object, guestRepo.Object);

        var result = await sut.Handle(new SearchPatientsQuery("all"), CancellationToken.None);

        Assert.Equal(10, result.Count);
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
            => action(ct);
    }
}
