using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Services.Wrappers;

namespace PiedraAzul.Client.Services.GraphQLServices;

public record CreateAppointmentGqlInput(
    string DoctorId,
    string DoctorAvailabilitySlotId,
    DateTime Date,
    string? PatientUserId = null,
    GuestPatientGqlInput? Guest = null
);

public record GuestPatientGqlInput(
    string Identification,
    string Name,
    string? Phone,
    string? ExtraInfo
);

public class GraphQLAppointmentService(GraphQLHttpClient client)
{
    public async Task<Result<AppointmentGQL>> CreateAppointment(CreateAppointmentGqlInput input)
    {
        const string mutation = """
            mutation CreateAppointment($input: CreateAppointmentInput!) {
                createAppointment(input: $input) {
                    id patientUserId patientGuestId patientType patientName
                    appointmentSlotId start createdAt
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<AppointmentGQL>(
                mutation,
                new { input },
                "createAppointment");
            return result!;
        });
    }

    public async Task<Result<List<AppointmentGQL>>> GetDoctorAppointments(
        string doctorId,
        DateTime? date = null)
    {
        const string query = """
            query GetDoctorAppointments($doctorId: String!, $date: DateTime) {
                doctorAppointments(doctorId: $doctorId, date: $date) {
                    id patientUserId patientGuestId patientType patientName
                    appointmentSlotId start createdAt
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<List<AppointmentGQL>>(
                query,
                new
                {
                    doctorId,
                    date = date.HasValue ? (object?)date.Value.ToUniversalTime().ToString("o") : null
                },
                "doctorAppointments");
            return result ?? new();
        });
    }
}
