using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.GraphQL;

public partial class Query
{
    /// <summary>
    /// Devuelve la ventana de reserva personalizada de un doctor (en semanas).
    /// Cada doctor puede tener un valor distinto (default: 4).
    /// </summary>
    public async Task<int> GetBookingWindowWeeksAsync(
        string doctorId,
        [Service] IDoctorRepository doctorRepository)
    {
        if (string.IsNullOrWhiteSpace(doctorId))
            throw new GraphQLException("doctorId es requerido");

        var doctor = await doctorRepository.GetByIdAsync(doctorId);
        return doctor?.BookingWindowWeeks ?? 4;
    }
}
