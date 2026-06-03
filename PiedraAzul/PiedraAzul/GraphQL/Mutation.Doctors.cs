using HotChocolate;
using HotChocolate.Authorization;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.GraphQL.Types;
using PiedraAzul.Infrastructure.Persistence;
using System.Security.Claims;

namespace PiedraAzul.GraphQL;

public partial class Mutation
{
    [Authorize(Roles = new[] { "Doctor" })]
    public async Task<DoctorAvailabilityType> ToggleMyAvailabilityAsync(
        ClaimsPrincipal claimsPrincipal,
        [Service] IDoctorRepository doctorRepository,
        [Service] AppDbContext dbContext)
    {
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var doctor = await doctorRepository.GetByIdAsync(userId)
            ?? throw new GraphQLException("No se encontró el perfil de doctor.");

        doctor.SetAvailability(!doctor.IsAvailable);

        await doctorRepository.UpdateAsync(doctor);
        await dbContext.SaveChangesAsync();

        return new DoctorAvailabilityType { DoctorId = doctor.Id, IsAvailable = doctor.IsAvailable };
    }
}

public class DoctorAvailabilityType
{
    public string DoctorId { get; set; } = "";
    public bool IsAvailable { get; set; }
}
