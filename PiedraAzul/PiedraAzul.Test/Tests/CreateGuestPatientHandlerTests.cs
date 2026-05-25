using Moq;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Features.Patients.Commands.CreateGuestPatient;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class CreateGuestPatientHandlerTests
{
    private readonly Mock<IPatientGuestRepository> _repo = new();

    private readonly CreateGuestPatientHandler _sut;

    public CreateGuestPatientHandlerTests()
    {
        _sut = new CreateGuestPatientHandler(_repo.Object, new ImmediateUnitOfWork());
    }

    [Fact]
    public async Task CreateGuestPatient_WithIdentification_UsesIdentificationAsId()
    {
        GuestPatient? saved = null;
        _repo.Setup(x => x.AddAsync(It.IsAny<GuestPatient>(), It.IsAny<CancellationToken>()))
            .Callback<GuestPatient, CancellationToken>((p, _) => saved = p);

        var result = await _sut.Handle(
            new CreateGuestPatientCommand("ced-123", "Juan", "3001234567", "", null),
            CancellationToken.None);

        Assert.Equal("ced-123", result);
        Assert.NotNull(saved);
        Assert.Equal("ced-123", saved!.Id);
    }

    [Fact]
    public async Task CreateGuestPatient_WithoutIdentification_GeneratesGuidAsId()
    {
        GuestPatient? saved = null;
        _repo.Setup(x => x.AddAsync(It.IsAny<GuestPatient>(), It.IsAny<CancellationToken>()))
            .Callback<GuestPatient, CancellationToken>((p, _) => saved = p);

        var result = await _sut.Handle(
            new CreateGuestPatientCommand("", "Juan", "3001234567", "", null),
            CancellationToken.None);

        Assert.True(Guid.TryParse(result, out _));
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateGuestPatient_WithWhitespaceIdentification_GeneratesGuid()
    {
        GuestPatient? saved = null;
        _repo.Setup(x => x.AddAsync(It.IsAny<GuestPatient>(), It.IsAny<CancellationToken>()))
            .Callback<GuestPatient, CancellationToken>((p, _) => saved = p);

        var result = await _sut.Handle(
            new CreateGuestPatientCommand("   ", "Juan", "3001234567", "", null),
            CancellationToken.None);

        Assert.True(Guid.TryParse(result, out _));
    }

    [Fact]
    public async Task CreateGuestPatient_AddsPatientToRepository()
    {
        _repo.Setup(x => x.AddAsync(It.IsAny<GuestPatient>(), It.IsAny<CancellationToken>()));

        await _sut.Handle(
            new CreateGuestPatientCommand("id-001", "Ana", "3009876543", "extra", "ana@test.com"),
            CancellationToken.None);

        _repo.Verify(x => x.AddAsync(
            It.Is<GuestPatient>(p => p.Id == "id-001" && p.Name == "Ana"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateGuestPatient_ReturnsPatientId()
    {
        _repo.Setup(x => x.AddAsync(It.IsAny<GuestPatient>(), It.IsAny<CancellationToken>()));

        var result = await _sut.Handle(
            new CreateGuestPatientCommand("id-002", "Pedro", "300", "", null),
            CancellationToken.None);

        Assert.Equal("id-002", result);
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
            => action(ct);
    }
}
