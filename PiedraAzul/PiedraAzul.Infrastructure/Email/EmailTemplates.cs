using System.Globalization;

namespace PiedraAzul.Infrastructure.Email;

/// <summary>
/// Plantillas de correo con un diseño único y consistente, alineado a la UI moderna
/// de la app (paleta #257D8D, tarjetas redondeadas, tipografía clara y legible).
/// Todas comparten el mismo "shell" (encabezado de marca + tarjeta + pie).
/// </summary>
public static class EmailTemplates
{
    // ── Paleta (UI moderna) ──────────────────────────────────────
    private const string Brand = "#257D8D";
    private const string Amber = "#D97706"; // reagendado
    private const string Rose  = "#E11D48"; // cancelado / alerta
    private const string Ink   = "#0F172A";
    private const string Body  = "#475569";
    private const string Muted = "#94A3B8";
    private const string Line  = "#E2E8F0";
    private const string Soft  = "#F1F5F9";
    private const string Bg    = "#EEF2F6";

    private static readonly CultureInfo Es = new("es-ES");

    // ── Shell común ──────────────────────────────────────────────
    private static string Shell(string title, string heading, string accent, string inner) => $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
</head>
<body style='margin:0;padding:0;background:{Bg};'>
    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:{Bg};font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;'>
        <tr><td align='center' style='padding:32px 12px;'>
            <table role='presentation' width='560' cellpadding='0' cellspacing='0' style='width:560px;max-width:100%;'>

                <!-- Marca -->
                <tr><td style='padding:0 6px 16px;font-size:20px;font-weight:800;color:{Brand};letter-spacing:-0.3px;'>
                    Piedra Azul <span style='color:{Muted};font-weight:600;font-size:13px;'>· Clínica</span>
                </td></tr>

                <!-- Tarjeta -->
                <tr><td style='background:#ffffff;border:1px solid {Line};border-radius:18px;'>
                    <table role='presentation' width='100%' cellpadding='0' cellspacing='0'>
                        <tr><td style='background:{accent};border-radius:18px 18px 0 0;padding:24px 32px;'>
                            <h1 style='margin:0;font-size:20px;font-weight:700;color:#ffffff;'>{heading}</h1>
                        </td></tr>
                        <tr><td style='padding:28px 32px;color:{Ink};font-size:15px;line-height:1.65;'>
                            {inner}
                        </td></tr>
                    </table>
                </td></tr>

                <!-- Pie -->
                <tr><td style='padding:18px 10px;text-align:center;color:{Muted};font-size:12px;line-height:1.7;'>
                    © 2026 Piedra Azul · Medicina alternativa y terapias especializadas<br>
                    Este es un mensaje automático, por favor no respondas a este correo.
                </td></tr>

            </table>
        </td></tr>
    </table>
</body>
</html>";

    // ── Bloques reutilizables ────────────────────────────────────
    private static string Greeting(string name) =>
        $"<p style='margin:0 0 14px;'>Hola{(string.IsNullOrWhiteSpace(name) ? "" : $" <strong>{name}</strong>")},</p>";

    private static string Para(string text, bool muted = false) =>
        $"<p style='margin:0 0 14px;color:{(muted ? Body : Ink)};'>{text}</p>";

    private static string Note(string text) =>
        $"<p style='margin:18px 0 0;color:{Muted};font-size:12.5px;line-height:1.6;'>{text}</p>";

    private static string Button(string label, string href) => $@"
        <table role='presentation' cellpadding='0' cellspacing='0' style='margin:22px 0;'>
            <tr><td style='background:{Brand};border-radius:10px;'>
                <a href='{href}' style='display:inline-block;padding:13px 28px;color:#ffffff;font-size:15px;font-weight:700;text-decoration:none;'>{label}</a>
            </td></tr>
        </table>";

    private static string CodeBox(string code) => $@"
        <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='margin:18px 0;'>
            <tr><td style='background:#F0FBFD;border:1.5px solid {Brand};border-radius:14px;padding:22px;text-align:center;'>
                <div style='font-size:34px;font-weight:800;letter-spacing:8px;color:{Brand};font-family:Consolas,Courier New,monospace;'>{code}</div>
            </td></tr>
        </table>";

    private static string InfoTable(params (string Label, string Value)[] rows)
    {
        var body = "";
        for (var i = 0; i < rows.Length; i++)
        {
            var border = i == 0 ? "" : $"border-top:1px solid {Soft};";
            body += $@"<tr>
                <td style='padding:12px 0;color:{Body};{border}'>{rows[i].Label}</td>
                <td style='padding:12px 0;color:{Ink};font-weight:700;text-align:right;{border}'>{rows[i].Value}</td>
            </tr>";
        }
        return $@"<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='margin:6px 0 4px;font-size:15px;'>{body}</table>";
    }

    // ── Plantillas de autenticación / cuenta ─────────────────────
    public static string PasswordResetTemplate(string userName, string resetLink) =>
        Shell("Restablecer contraseña", "Restablece tu contraseña", Brand,
            Greeting(userName) +
            Para("Recibimos una solicitud para restablecer tu contraseña en Piedra Azul. Pulsa el botón para crear una nueva:") +
            Button("Restablecer mi contraseña", resetLink) +
            Para("Si el botón no funciona, copia y pega este enlace en tu navegador:", muted: true) +
            $"<p style='margin:0;color:{Brand};font-size:13px;word-break:break-all;'>{resetLink}</p>" +
            Note("El enlace vence en 24 horas. Si no solicitaste el cambio, puedes ignorar este correo: tu contraseña no cambiará."));

    public static string MFAVerificationTemplate(string userName, string otp, int expirationMinutes) =>
        Shell("Código de verificación", "Tu código de acceso", Brand,
            Greeting(userName) +
            Para("Solicitaste iniciar sesión en Piedra Azul. Ingresa este código para continuar:") +
            CodeBox(otp) +
            Para($"El código vence en <strong>{expirationMinutes} minutos</strong>.", muted: true) +
            Note("Si no fuiste tú, ignora este correo; tu cuenta sigue protegida."));

    public static string AccountLockedTemplate(string userName) =>
        Shell("Cuenta bloqueada", "Cuenta bloqueada temporalmente", Rose,
            Greeting(userName) +
            Para("Tu cuenta se bloqueó temporalmente tras varios intentos de inicio de sesión fallidos. Es una medida de seguridad para protegerte.") +
            Para("Se desbloqueará automáticamente en <strong>15 minutos</strong>. Después podrás intentar de nuevo.", muted: true) +
            Note("Si no fuiste tú quien intentó entrar, te recomendamos cambiar tu contraseña en cuanto recuperes el acceso."));

    public static string WelcomeWithPasswordTemplate(string userName, string email, string tempPassword, string roleLabel) =>
        Shell("Bienvenido a Piedra Azul", "¡Tu cuenta está lista!", Brand,
            Greeting(userName) +
            Para($"El administrador creó una cuenta para ti en Piedra Azul con el rol de <strong>{roleLabel}</strong>. Estos son tus datos de acceso:") +
            InfoTable(("Correo", email)) +
            CodeBox(tempPassword) +
            Para("⚠️ Por tu seguridad, cambia esta contraseña temporal la primera vez que inicies sesión.", muted: true) +
            Button("Iniciar sesión", "https://piedraazul.com") +
            Note("Si no esperabas esta cuenta, comunícate con la clínica."));

    public static string MFASetupConfirmationTemplate(string userName, string mfaMethod) =>
        Shell("Verificación en dos pasos activada", "Verificación en dos pasos activada", Brand,
            Greeting(userName) +
            Para($"Activaste la verificación en dos pasos usando <strong>{mfaMethod}</strong>. ¡Buena decisión! Tu cuenta ahora es más segura.") +
            Para("A partir de ahora te pediremos un código adicional cada vez que inicies sesión.", muted: true) +
            Note("Guarda tus códigos de recuperación en un lugar seguro por si pierdes el acceso a tu método de verificación."));

    // ── Plantillas de citas ──────────────────────────────────────
    public static string AppointmentCreatedTemplate(string patientName, string doctorName, DateTime start) =>
        AppointmentShell(
            patientName, doctorName, start,
            title: "Tu cita ha sido agendada",
            heading: "¡Tu cita está confirmada!",
            accent: Brand,
            intro: "Hemos registrado tu cita médica con los siguientes datos:",
            note: "Si necesitas cambiarla o cancelarla, entra a «Mis citas» en tu cuenta. Te esperamos.");

    public static string AppointmentRescheduledTemplate(string patientName, string doctorName, DateTime start) =>
        AppointmentShell(
            patientName, doctorName, start,
            title: "Tu cita ha sido reagendada",
            heading: "Tu cita fue reagendada",
            accent: Amber,
            intro: "Tu cita médica cambió de fecha. Estos son los nuevos datos:",
            note: "Si esta nueva fecha no te conviene, puedes volver a reagendarla desde «Mis citas».");

    public static string AppointmentCancelledTemplate(string patientName, string doctorName, DateTime start) =>
        AppointmentShell(
            patientName, doctorName, start,
            title: "Tu cita ha sido cancelada",
            heading: "Tu cita fue cancelada",
            accent: Rose,
            intro: "Te confirmamos que la siguiente cita ha sido cancelada:",
            note: "¿Fue un error o quieres otra fecha? Puedes agendar una nueva cita cuando quieras desde tu cuenta.");

    private static string AppointmentShell(
        string patientName, string doctorName, DateTime start,
        string title, string heading, string accent, string intro, string note)
    {
        var dateStr = char.ToUpper(start.ToString("dddd", Es)[0])
                      + start.ToString("dddd, d 'de' MMMM 'de' yyyy", Es)[1..];
        var timeStr = start.ToString("hh:mm tt", CultureInfo.InvariantCulture);

        return Shell(title, heading, accent,
            Greeting(patientName) +
            Para(intro) +
            InfoTable(
                ("Especialista", doctorName),
                ("Fecha", dateStr),
                ("Hora", timeStr)) +
            Note(note));
    }
}
