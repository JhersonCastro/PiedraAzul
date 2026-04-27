namespace PiedraAzul.Infrastructure.Email;

public static class EmailTemplates
{
    public static string PasswordResetTemplate(string userName, string resetLink) => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Restablecer Contraseña</title>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px;'>
        <h2 style='color: #257D8D;'>Restablecer Contraseña</h2>
        <p>Hola {userName},</p>
        <p>Recibimos una solicitud para restablecer tu contraseña en Piedra Azul.</p>
        <p>Haz clic en el siguiente botón para restablecer tu contraseña:</p>
        <a href='{resetLink}' style='display: inline-block; background-color: #257D8D; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 20px 0;'>
            Restablecer Contraseña
        </a>
        <p style='color: #666;'>O copia y pega este enlace en tu navegador:</p>
        <p style='color: #666; word-break: break-all;'>{resetLink}</p>
        <p style='color: #999; font-size: 12px;'>Este enlace expira en 24 horas.</p>
        <p style='color: #999; font-size: 12px;'>Si no solicitaste este cambio, ignora este correo.</p>
        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
        <p style='color: #999; font-size: 11px;'>© 2024 Piedra Azul. Todos los derechos reservados.</p>
    </div>
</body>
</html>";

    public static string MFAVerificationTemplate(string userName, string otp, int expirationMinutes) => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Verificación de dos factores</title>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px;'>
        <h2 style='color: #257D8D;'>Código de Verificación</h2>
        <p>Hola {userName},</p>
        <p>Solicitaste iniciar sesión en Piedra Azul. Para continuar, ingresa el siguiente código:</p>
        <div style='background-color: #f9f9f9; padding: 20px; border-radius: 4px; text-align: center; margin: 20px 0;'>
            <p style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #257D8D; margin: 0;'>{otp}</p>
        </div>
        <p style='color: #666;'>Este código expira en {expirationMinutes} minutos.</p>
        <p style='color: #999; font-size: 12px;'>Si no solicitaste este código, ignora este correo.</p>
        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
        <p style='color: #999; font-size: 11px;'>© 2024 Piedra Azul. Todos los derechos reservados.</p>
    </div>
</body>
</html>";

    public static string AccountLockedTemplate(string userName) => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Cuenta Bloqueada</title>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px;'>
        <h2 style='color: #d32f2f;'>Cuenta Bloqueada Temporalmente</h2>
        <p>Hola {userName},</p>
        <p>Tu cuenta ha sido bloqueada temporalmente debido a varios intentos de inicio de sesión fallidos.</p>
        <p style='color: #666;'>Tu cuenta se desbloqueará automáticamente en 15 minutos.</p>
        <p style='color: #999; font-size: 12px;'>Si no fue tu, contacta con nosotros inmediatamente.</p>
        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
        <p style='color: #999; font-size: 11px;'>© 2024 Piedra Azul. Todos los derechos reservados.</p>
    </div>
</body>
</html>";

    public static string MFASetupConfirmationTemplate(string userName, string mfaMethod) => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Verificación de dos factores activada</title>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px;'>
        <h2 style='color: #257D8D;'>Verificación de dos factores activada</h2>
        <p>Hola {userName},</p>
        <p>Tu verificación de dos factores ha sido activada exitosamente usando {mfaMethod}.</p>
        <p style='color: #666;'>Ahora necesitarás un código de verificación adicional cada vez que inicies sesión.</p>
        <p style='color: #999; font-size: 12px;'>Guarda tus códigos de recuperación en un lugar seguro.</p>
        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
        <p style='color: #999; font-size: 11px;'>© 2024 Piedra Azul. Todos los derechos reservados.</p>
    </div>
</body>
</html>";
}
