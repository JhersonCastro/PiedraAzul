using Moq;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.User;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorsBySpecialty;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class GetDoctorsBySpecialtyHandlerTests
{
    private readonly Mock<IDoctorRepository> _doctorRepository = new();
    private readonly Mock<IIdentityService> _identityService = new();

    private readonly GetDoctorsBySpecialtyHandler _sut;

    public GetDoctorsBySpecialtyHandlerTests()
    {
        _sut = new GetDoctorsBySpecialtyHandler(_doctorRepository.Object, _identityService.Object);
    }

    [Fact]
    public async Task GetDoctorsBySpecialty_WhenNoDoctors_ReturnsEmpty()
    {
        _doctorRepository
            .Setup(x => x.GetBySpecialtyAsync(DoctorType.NaturalMedicine, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(
            new GetDoctorsBySpecialtyQuery(DoctorType.NaturalMedicine),
            CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDoctorsBySpecialty_MapsDoctorWithUserInfo()
    {
        var doctor = new Doctor("doc-1", DoctorType.NaturalMedicine, "LIC-001", "Notes");

        _doctorRepository
            .Setup(x => x.GetBySpecialtyAsync(DoctorType.NaturalMedicine, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([doctor]);

        // Cambiar string[] a List<string>
        _identityService
            .Setup(x => x.GetByIds(new List<string> { "doc-1" }))
            .ReturnsAsync([new UserDto("doc-1", "doc@test.com", "Dr. Garcia", "avatar.png", true)]);

        var result = await _sut.Handle(
            new GetDoctorsBySpecialtyQuery(DoctorType.NaturalMedicine),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Dr. Garcia", result[0].Name);
        Assert.Equal("LIC-001", result[0].LicenseNumber);
        Assert.Equal(DoctorType.NaturalMedicine, result[0].Specialty);
    }

    [Fact]
    public async Task GetDoctorsBySpecialty_FiltersOutDoctorsWithoutUserInfo()
    {
        var doctor1 = new Doctor("doc-1", DoctorType.NaturalMedicine, "LIC-001", "");
        var doctor2 = new Doctor("doc-2", DoctorType.NaturalMedicine, "LIC-002", "");

        _doctorRepository
            .Setup(x => x.GetBySpecialtyAsync(DoctorType.NaturalMedicine, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([doctor1, doctor2]);

        // Cambiar It.IsAny<IEnumerable<string>>() a It.IsAny<List<string>>()
        _identityService
            .Setup(x => x.GetByIds(It.IsAny<List<string>>()))
            .ReturnsAsync([new UserDto("doc-1", "doc1@test.com", "Dr. One", "", true)]);

        var result = await _sut.Handle(
            new GetDoctorsBySpecialtyQuery(DoctorType.NaturalMedicine),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("doc-1", result[0].Id);
    }

    [Fact]
    public async Task GetDoctorsBySpecialty_DoesNotQueryIdentityWhenNoDoctors()
    {
        _doctorRepository
            .Setup(x => x.GetBySpecialtyAsync(It.IsAny<DoctorType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.Handle(
            new GetDoctorsBySpecialtyQuery(DoctorType.NaturalMedicine), // valor válido
            CancellationToken.None);

        // Verificar que nunca se llame a GetByIds con cualquier lista
        _identityService.Verify(x => x.GetByIds(It.IsAny<List<string>>()), Times.Never);
    }
}