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
    string? ExtraInfo,
    string? Email = null
);

public class GraphQLAppointmentService(GraphQLHttpClient client)
{
    public async Task<Result<AppointmentGQL>> CreateAppointment(CreateAppointmentGqlInput input)
    {
        const string mutation = """
            mutation CreateAppointment($input: CreateAppointmentInput!) {
                createAppointment(input: $input) {
                    id patientUserId patientGuestId patientType patientName
                    appointmentSlotId doctorId doctorName specialty start createdAt
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

    /// <summary>
    /// Crea una cita como invitado sin autenticación (flujo InstantBooking).
    /// Llama a la mutación pública bookGuestAppointment.
    /// </summary>
    public async Task<Result<AppointmentGQL>> BookGuestAppointmentAsync(CreateAppointmentGqlInput input)
    {
        const string mutation = """
            mutation BookGuestAppointment($input: CreateAppointmentInput!) {
                bookGuestAppointment(input: $input) {
                    id patientUserId patientGuestId patientType patientName
                    appointmentSlotId doctorId doctorName specialty start createdAt
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<AppointmentGQL>(
                mutation,
                new { input },
                "bookGuestAppointment");
            return result!;
        });
    }

    public async Task<Result<string>> SendGuestOtpAsync(string phone, string? email, string channel)
    {
        const string mutation = """
            mutation SendGuestOtp($phone: String!, $email: String, $channel: String!) {
                sendGuestOtp(phone: $phone, email: $email, channel: $channel)
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<string>(
                mutation,
                new { phone, email, channel },
                "sendGuestOtp");
            return result!;
        });
    }

    public async Task<Result<bool>> VerifyGuestOtpAsync(string sessionToken, string code)
    {
        const string mutation = """
            mutation VerifyGuestOtp($sessionToken: String!, $code: String!) {
                verifyGuestOtp(sessionToken: $sessionToken, code: $code)
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<bool>(
                mutation,
                new { sessionToken, code },
                "verifyGuestOtp");
            return result;
        });
    }

    public async Task<Result<bool>> SendGuestOtpByHashAsync(string hash, string channel)
    {
        const string mutation = """
            mutation SendGuestOtpByHash($hash: String!, $channel: String!) {
                sendGuestOtpByHash(hash: $hash, channel: $channel)
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            await client.ExecuteAsync<object?>(mutation, new { hash, channel }, "sendGuestOtpByHash");
            return true;
        });
    }

    /// <summary>
    /// Verifica OTP por hash y opcionalmente actualiza datos editados por el usuario.
    /// Si name/phone/email son null, solo verifica el OTP.
    /// </summary>
    public async Task<Result<GuestDataGQL?>> VerifyGuestOtpByHashAsync(
        string hash, string code, string? name = null, string? phone = null, string? email = null)
    {
        const string mutation = """
            mutation VerifyGuestOtpByHash($hash: String!, $code: String!, $name: String, $phone: String, $email: String) {
                verifyGuestOtpByHash(hash: $hash, code: $code, name: $name, phone: $phone, email: $email) {
                    id name phone email sessionType
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
            await client.ExecuteAsync<GuestDataGQL?>(mutation, new { hash, code, name, phone, email }, "verifyGuestOtpByHash")
        );
    }

    public async Task<Result<List<AppointmentGQL>>> GetMyAppointmentsAsync()
    {
        const string query = """
            query GetMyAppointments {
                myAppointments {
                    id patientUserId patientGuestId patientType patientName
                    appointmentSlotId doctorId doctorName specialty start createdAt status
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<List<AppointmentGQL>>(
                query,
                null,
                "myAppointments");
            return result ?? new();
        });
    }

    public async Task<Result<List<AppointmentGQL>>> GetMyUpcomingAppointmentsAsync()
    {
        const string query = """
            query GetMyUpcomingAppointments {
                myUpcomingAppointments {
                    id patientUserId patientGuestId patientType patientName
                    appointmentSlotId doctorId doctorName specialty start createdAt status
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<List<AppointmentGQL>>(
                query,
                null,
                "myUpcomingAppointments");
            return result ?? new();
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
                    appointmentSlotId doctorId doctorName specialty start createdAt status
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
                    // Usamos DateTimeKind.Utc en la fecha (sin hora) para evitar
                    // que ToUniversalTime cambie el día en zonas UTC+X
                    date = date.HasValue
                        ? (object?)DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc).ToString("o")
                        : null
                },
                "doctorAppointments");
            return result ?? new();
        });
    }

    /// <summary>Reagenda una cita para un paciente autenticado.</summary>
    public async Task<Result<AppointmentGQL>> RescheduleAppointmentAsync(
        Guid appointmentId, Guid newSlotId, DateTime newDate)
    {
        const string mutation = """
            mutation RescheduleAppointment($appointmentId: UUID!, $newSlotId: UUID!, $newDate: DateTime!) {
                rescheduleAppointment(appointmentId: $appointmentId, newSlotId: $newSlotId, newDate: $newDate) {
                    id patientUserId patientGuestId patientType patientName
                    appointmentSlotId doctorId doctorName specialty start createdAt status
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<AppointmentGQL>(
                mutation,
                new { appointmentId, newSlotId, newDate },
                "rescheduleAppointment");
            return result!;
        });
    }

    /// <summary>Reagenda una cita para un invitado verificado por hash.</summary>
    public async Task<Result<AppointmentGQL>> RescheduleAppointmentByHashAsync(
        string verificationHash, Guid appointmentId, Guid newSlotId, DateTime newDate)
    {
        const string mutation = """
            mutation RescheduleAppointmentByHash($verificationHash: String!, $appointmentId: UUID!, $newSlotId: UUID!, $newDate: DateTime!) {
                rescheduleAppointmentByHash(verificationHash: $verificationHash, appointmentId: $appointmentId, newSlotId: $newSlotId, newDate: $newDate) {
                    id patientUserId patientGuestId patientType patientName
                    appointmentSlotId doctorId doctorName specialty start createdAt status
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<AppointmentGQL>(
                mutation,
                new { verificationHash, appointmentId, newSlotId, newDate },
                "rescheduleAppointmentByHash");
            return result!;
        });
    }

    /// <summary>Obtiene las citas próximas de un invitado verificado por hash.</summary>
    public async Task<Result<List<AppointmentGQL>>> GetGuestUpcomingAppointmentsAsync(string verificationHash)
    {
        const string query = """
            query GetGuestUpcomingAppointments($verificationHash: String!) {
                guestUpcomingAppointments(verificationHash: $verificationHash) {
                    id patientUserId patientGuestId patientType patientName
                    appointmentSlotId doctorId doctorName specialty start createdAt status
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<List<AppointmentGQL>>(
                query,
                new { verificationHash },
                "guestUpcomingAppointments");
            return result ?? new();
        });
    }
}
