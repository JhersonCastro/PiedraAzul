using Moq;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.User;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorByUserId;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task GetDoctorByUserId_MapsAvatarAndSpecialty()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var identity = new Mock<IIdentityService>();

        doctorRepo.Setup(x => x.GetByIdAsync("doc-x", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor("doc-x", DoctorType.Chiropractic, "LIC-X", "Perfil"));
        identity.Setup(x => x.GetById("doc-x"))
            .ReturnsAsync(new UserDto("doc-x", "docx@test.com", "Doc X", "x.png"));

        var sut = new GetDoctorByUserIdHandler(doctorRepo.Object, identity.Object);

        var result = await sut.Handle(new GetDoctorByUserIdQuery("doc-x"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("x.png", result!.AvatarUrl);
        Assert.Equal(DoctorType.Chiropractic, result.Specialty);
    }

    [Fact]
    public async Task GetDoctorByUserId_UsesDoctorLicenseNumber()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var identity = new Mock<IIdentityService>();

        doctorRepo.Setup(x => x.GetByIdAsync("doc-y", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor("doc-y", DoctorType.Optometry, "LIC-999", ""));
        identity.Setup(x => x.GetById("doc-y"))
            .ReturnsAsync(new UserDto("doc-y", "docy@test.com", "Doc Y", "y.png"));

        var sut = new GetDoctorByUserIdHandler(doctorRepo.Object, identity.Object);

        var result = await sut.Handle(new GetDoctorByUserIdQuery("doc-y"), CancellationToken.None);

        Assert.Equal("LIC-999", result!.LicenseNumber);
    }

    [Fact]
    public async Task GetDoctorByUserId_UsesIdentityNameInsteadOfDomainName()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var identity = new Mock<IIdentityService>();

        doctorRepo.Setup(x => x.GetByIdAsync("doc-z", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor("doc-z", DoctorType.NaturalMedicine, "LIC-100", ""));
        identity.Setup(x => x.GetById("doc-z"))
            .ReturnsAsync(new UserDto("doc-z", "docz@test.com", "Nombre Visible", "z.png"));

        var sut = new GetDoctorByUserIdHandler(doctorRepo.Object, identity.Object);

        var result = await sut.Handle(new GetDoctorByUserIdQuery("doc-z"), CancellationToken.None);

        Assert.Equal("Nombre Visible", result!.Name);
    }

    [Fact]
    public async Task GetDoctorByUserId_ReturnsNull_WhenIdentityReturnsNull()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var identity = new Mock<IIdentityService>();

        doctorRepo.Setup(x => x.GetByIdAsync("doc-10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor("doc-10", DoctorType.Chiropractic, "LIC-10", ""));
        identity.Setup(x => x.GetById("doc-10")).ReturnsAsync((UserDto?)null);

        var sut = new GetDoctorByUserIdHandler(doctorRepo.Object, identity.Object);

        var result = await sut.Handle(new GetDoctorByUserIdQuery("doc-10"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDoctorByUserId_CallsRepositoryOnce()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var identity = new Mock<IIdentityService>();

        doctorRepo.Setup(x => x.GetByIdAsync("doc-11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor("doc-11", DoctorType.Physiotherapy, "LIC-11", ""));
        identity.Setup(x => x.GetById("doc-11"))
            .ReturnsAsync(new UserDto("doc-11", "11@test.com", "Doc 11", "11.png"));

        var sut = new GetDoctorByUserIdHandler(doctorRepo.Object, identity.Object);

        await sut.Handle(new GetDoctorByUserIdQuery("doc-11"), CancellationToken.None);

        doctorRepo.Verify(x => x.GetByIdAsync("doc-11", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDoctorByUserId_CallsIdentityOnce()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var identity = new Mock<IIdentityService>();

        doctorRepo.Setup(x => x.GetByIdAsync("doc-12", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor("doc-12", DoctorType.NaturalMedicine, "LIC-12", ""));
        identity.Setup(x => x.GetById("doc-12"))
            .ReturnsAsync(new UserDto("doc-12", "12@test.com", "Doc 12", "12.png"));

        var sut = new GetDoctorByUserIdHandler(doctorRepo.Object, identity.Object);

        await sut.Handle(new GetDoctorByUserIdQuery("doc-12"), CancellationToken.None);

        identity.Verify(x => x.GetById("doc-12"), Times.Once);
    }
}
