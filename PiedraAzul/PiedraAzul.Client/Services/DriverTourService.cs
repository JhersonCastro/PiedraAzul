using Microsoft.JSInterop;
using PiedraAzul.Client.States;

namespace PiedraAzul.Client.Services;

public class DriverTourService(IJSRuntime js, UIModeState uiMode)
{
    private const string SetupKey = "uiModeSetup";

    /// <summary>Retorna true si el tour NO se ha visto (debe mostrarse). En modo fácil nunca se muestra.</summary>
    public async Task<bool> ShouldShowTourAsync(string tourId)
    {
        // Los tours apuntan a anclajes de la UI moderna; en modo fácil esa UI no existe.
        if (uiMode.IsEasy) return false;

        try
        {
            // No mostrar el tour si el usuario todavía no ha completado el selector de modo UI.
            // Evita que el tour de DriverJS y el modal de configuración se solapen en la primera visita.
            var setupDone = await js.InvokeAsync<string?>("localStorage.getItem", SetupKey);
            if (setupDone is null) return false;

            var seen = await js.InvokeAsync<bool>("DriverTour.isTourSeen", tourId);
            return !seen;
        }
        catch { return false; }
    }

    public async Task StartTourAsync(string tourId, IEnumerable<string>? roles = null, object? dotnetRef = null)
    {
        // En modo fácil no hay tours: la UI es distinta y no tiene los anclajes data-tour.
        if (uiMode.IsEasy) return;

        bool isMobile = false;
        try { isMobile = await js.InvokeAsync<bool>("DriverTour.isMobile"); } catch { }

        var steps = BuildSteps(tourId, roles ?? [], isMobile);
        if (steps is null || steps.Length == 0) return;
        try { await js.InvokeVoidAsync("DriverTour.startTour", steps, tourId, dotnetRef); }
        catch { }
    }

    public async Task SkipTourAsync(string tourId)
    {
        try { await js.InvokeVoidAsync("DriverTour.skipTour", tourId); }
        catch { }
    }

    public async Task DestroyAsync()
    {
        try { await js.InvokeVoidAsync("DriverTour.destroyTour"); }
        catch { }
    }

    private static object[]? BuildSteps(string tourId, IEnumerable<string> roles, bool isMobile = false) => tourId switch
    {
        TourIds.AccountProfile => AccountProfileTourSteps.Build(roles, isMobile),
        TourIds.HomeWelcome    => HomeWelcomeTourSteps.Build(),
        TourIds.BookingTour    => BookingTourSteps.Build(),
        _ => null
    };
}

// ─────────────────────────────────────────────
public static class TourIds
{
    public const string AccountProfile = "account-profile";
    public const string HomeWelcome    = "home-welcome";
    public const string BookingTour    = "booking-tour";
}

// ─────────────────────────────────────────────
internal static class AccountProfileTourSteps
{
    public static object[] Build(IEnumerable<string> roles, bool isMobile = false)
    {
        var roleSet = roles.Select(r => r.ToLowerInvariant()).ToHashSet();
        bool isPatient = roleSet.Contains("patient");
        bool isDoctor  = roleSet.Contains("doctor");
        bool isAdmin   = roleSet.Contains("admin");

        return isMobile
            ? BuildMobile(isPatient, isDoctor, isAdmin)
            : BuildDesktop(isPatient, isDoctor, isAdmin);
    }

    // ── MOBILE: apunta a elementos visibles en el contenido apilado y en el bottom nav ──
    // Los steps con `tab` disparan TourNavigateToTab → cambian _mobileTab → Blazor re-renderiza.
    // driver-tour.js espera 180ms tras la navegación antes de llamar a moveNext().
    private static object[] BuildMobile(bool isPatient, bool isDoctor, bool isAdmin)
    {
        var steps = new List<object>
        {
            // ── Bienvenida en tab Mi Cuenta (ya está activo por defecto) ──
            Step("[data-tour='mobile-tab-cuenta']", null,
                "Mi Cuenta",
                "Desde aquí gestionas tu perfil, seguridad y autenticación de dos factores.",
                "top"),

            // ── Acceso rápido: pills del sub-nav ──
            Step("[data-tour='mobile-subnav']", null,
                "Acceso rápido",
                "Estas pastillas te llevan directamente a cada sección sin tener que hacer scroll. Toca cualquiera para saltar al instante.",
                "top"),

            // ── Perfil ──
            Step("[data-tour='profile-info-card']", null,
                "Foto y nombre",
                "Toca el ícono del lápiz sobre el avatar para cambiar tu foto, o edita tu nombre directamente.",
                "bottom"),
            Step("[data-tour='profile-email-card']", null,
                "Correo electrónico",
                "Para cambiar tu email recibirás un código de verificación que mantiene tu cuenta segura.",
                "bottom"),

            // ── Seguridad ──
            Step("[data-tour='security-passkeys-card']", null,
                "Passkeys",
                "Registra Face ID, huella digital o llave de seguridad para entrar sin contraseña.",
                "bottom"),
            Step("[data-tour='security-password-card']", null,
                "Contraseña",
                "Si prefieres contraseña tradicional, puedes actualizarla aquí cuando lo necesites.",
                "bottom"),

            // ── 2FA ──
            Step("[data-tour='twofa-settings']", null,
                "Autenticación 2FA",
                "Activa la verificación en dos pasos para añadir una capa extra de protección a tu cuenta.",
                "bottom"),
        };

        // ── Paciente ──
        if (isPatient)
        {
            // tab: "pac-citas" → TourNavigateToTab → _mobileTab = "citas" → re-render
            steps.Add(Step("[data-tour='mobile-tab-citas']", "pac-citas",
                "Mis Citas",
                "Toca aquí para ver tus citas médicas: próximas, pasadas y su estado.",
                "top"));
            steps.Add(Step("[data-tour='patient-appointments-header']", null,
                "Tus citas",
                "Verás tus próximas citas. Puedes reagendar o cancelar las que aún no han ocurrido.",
                "bottom"));
        }

        // ── Doctor ──
        if (isDoctor)
        {
            // tab: "doc-dashboard" → TourNavigateToTab → _mobileTab = "doctor" → re-render
            steps.Add(Step("[data-tour='mobile-tab-doctor']", "doc-dashboard",
                "Portal Doctor",
                "Accede a tu panel médico, agenda de citas y lista de pacientes.",
                "top"));
            steps.Add(Step("[data-tour='doctor-status']", null,
                "Tu disponibilidad",
                "Cambia entre Activo e Inactivo para indicar si estás disponible para recibir pacientes hoy.",
                "bottom"));
            steps.Add(Step("[data-tour='doctor-stats']", null,
                "Resumen del día",
                "De un vistazo ves el total de citas de hoy, las completadas y las pendientes.",
                "bottom"));
            steps.Add(Step("[data-tour='doctor-agenda-calendar']", null,
                "Mi Agenda",
                "Navega entre meses para ver tus citas programadas. Los días marcados tienen citas.",
                "bottom"));
        }

        // ── Admin ──
        if (isAdmin)
        {
            // tab: "admin-usuarios" → TourNavigateToTab → _mobileTab = "admin" → re-render
            steps.Add(Step("[data-tour='mobile-tab-admin']", "admin-usuarios",
                "Administración",
                "Desde aquí gestionas usuarios, la agenda médica y los horarios de la clínica.",
                "top"));
            steps.Add(Step("[data-tour='admin-user-management-header']", null,
                "Gestión de usuarios",
                "Busca por nombre, email o rol. Crea nuevas cuentas y asigna roles a los usuarios.",
                "bottom"));
            steps.Add(Step("[data-tour='admin-agenda-filters']", null,
                "Agenda médica",
                "Filtra por médico y fecha para ver todas las citas del día y gestionar su estado.",
                "bottom"));
        }

        // ── Servicios (Doctor y/o Admin) ──
        if (isDoctor || isAdmin)
        {
            // tab: "agendar-manual" → TourNavigateToTab → _mobileTab = "servicios" → re-render
            steps.Add(Step("[data-tour='mobile-tab-servicios']", "agendar-manual",
                "Servicios",
                "Aquí puedes agendar citas manualmente para tus pacientes y exportar datos.",
                "top"));
            steps.Add(Step("[data-tour='manual-booking-header']", null,
                "Agendar manualmente",
                "Completa los datos del paciente, elige el médico y el horario para registrar la cita.",
                "bottom"));
        }

        // ── Fin — volver a "Mi Cuenta" para mostrar la pill de logout ──
        steps.Add(Step("[data-tour='mobile-logout-pill']", "perfil",
            "Cerrar sesión",
            "Cuando quieras salir, toca esta pastilla roja en la barra de acceso rápido. Te pedirá confirmación antes de cerrar tu sesión.",
            "top"));

        return [.. steps];
    }

    // ── DESKTOP: pasos originales con sidebar y sub-navegación ──
    private static object[] BuildDesktop(bool isPatient, bool isDoctor, bool isAdmin)
    {
        // Prefijo para apuntar SOLO al contenido desktop y evitar el elemento del div mobile (md:hidden)
        const string D = "#pa-desktop-main ";

        var steps = new List<object>
        {
            Step("[data-tour='sidebar-user-card']", null, "Tu perfil",
                "Aquí puedes ver tu avatar, nombre, email y los roles asignados en la clínica.",
                "right", "start"),

            Step("[data-tour='nav-perfil']", "perfil", "Mi Perfil",
                "Haz clic aquí para gestionar tu información personal.", "right"),
            Step(D + "[data-tour='profile-info-card']", null, "Foto y nombre",
                "Cambia tu foto de perfil haciendo clic en el ícono del lápiz sobre el avatar. También puedes editar tu nombre directamente aquí.", "top"),
            Step(D + "[data-tour='profile-email-card']", null, "Correo electrónico",
                "Para cambiar tu email se envía un código de verificación. Esto mantiene tu cuenta segura.", "top"),

            Step("[data-tour='nav-seguridad']", "seguridad", "Seguridad",
                "Administra tus métodos de acceso y autenticación.", "right"),
            Step(D + "[data-tour='security-passkeys-card']", null, "Passkeys",
                "Registra una passkey (Face ID, huella o llave USB) para entrar sin contraseña desde dispositivos de confianza.", "top"),
            Step(D + "[data-tour='security-password-card']", null, "Contraseña",
                "Si prefieres usar contraseña, puedes cambiarla aquí cuando lo necesites.", "top"),

            Step("[data-tour='nav-2fa']", "autenticacion", "Autenticación 2FA",
                "Añade una segunda capa de seguridad a tu cuenta.", "right"),
            Step(D + "[data-tour='twofa-settings']", null, "Configurar 2FA",
                "Activa o desactiva la autenticación de dos factores. Puedes usar tu email como segundo factor.", "top"),
        };

        if (isPatient)
        {
            steps.Add(Step("[data-tour='nav-mis-citas']", "pac-citas", "Mis Citas",
                "Consulta el historial y próximas citas médicas.", "right"));
            steps.Add(Step(D + "[data-tour='patient-appointments-header']", null, "Tus citas",
                "Aquí verás todas tus citas: próximas, pasadas y su estado. Puedes cancelar las futuras si lo necesitas.", "bottom"));
        }

        if (isDoctor)
        {
            steps.Add(Step("[data-tour='nav-doc-dashboard']", "doc-dashboard", "Portal Doctor — Dashboard",
                "Tu panel principal como médico. Aquí ves el resumen del día y accesos rápidos a cada sección.", "right"));
            steps.Add(Step(D + "[data-tour='doctor-status']", null, "Tu disponibilidad",
                "Indica si estás disponible para recibir pacientes hoy. Cambia entre Activo e Inactivo con un solo clic.", "bottom"));
            steps.Add(Step(D + "[data-tour='doctor-stats']", null, "Resumen del día",
                "De un vistazo ves el total de citas, cuántas ya completaste y cuántas están pendientes para hoy.", "top"));
            steps.Add(Step(D + "[data-tour='doctor-quick-actions']", null, "Accesos rápidos",
                "Desde aquí puedes ir directamente a tu agenda, cronograma del día, lista de pacientes o configurar tu horario.", "top"));
            steps.Add(Step("[data-tour='nav-doc-agenda']", "doc-agenda", "Mi Agenda",
                "Visualiza y gestiona todas tus citas programadas.", "right"));
            steps.Add(Step(D + "[data-tour='doctor-agenda-calendar']", null, "Calendario mensual",
                "Navega entre meses para ver tus citas. Las marcas de color indican los días con citas programadas.", "top"));
            steps.Add(Step("[data-tour='nav-agendar-manual']", "agendar-manual", "Agendar manualmente",
                "Registra citas para tus pacientes directamente desde aquí.", "right"));
            steps.Add(Step(D + "[data-tour='manual-booking-header']", null, "Formulario de agendamiento",
                "Completa los datos del paciente, elige el médico y el horario disponible para registrar la cita.", "bottom"));
        }

        if (isAdmin)
        {
            steps.Add(Step("[data-tour='nav-admin-usuarios']", "admin-usuarios", "Gestión de usuarios",
                "Crea cuentas, asigna roles y gestiona el acceso de todos los usuarios de la clínica.", "right"));
            steps.Add(Step(D + "[data-tour='admin-user-management-header']", null, "Panel de usuarios",
                "Busca por nombre, email o teléfono y filtra por rol. Desde aquí también puedes crear nuevas cuentas.", "bottom"));
            steps.Add(Step("[data-tour='nav-admin-agenda']", "admin-agenda", "Agenda médica",
                "Vista global de todas las citas. Puedes filtrar por médico, fecha o estado.", "right"));
            steps.Add(Step(D + "[data-tour='admin-agenda-filters']", null, "Filtros de agenda",
                "Selecciona un especialista y una fecha para ver sus citas del día y gestionar el estado de cada una.", "bottom"));
            steps.Add(Step("[data-tour='nav-agendar-manual']", "agendar-manual", "Agendar manualmente",
                "Registra citas para cualquier paciente asignándolas al médico y horario que prefieras.", "right"));
            steps.Add(Step(D + "[data-tour='manual-booking-header']", null, "Formulario de agendamiento",
                "Completa los datos del paciente, elige el médico y el horario disponible para registrar la cita.", "bottom"));
        }

        steps.Add(Step("[data-tour='logout-button']", null, "¡Todo listo!",
            "Ya conoces tu cuenta. Cuando termines, cierra sesión de forma segura desde aquí.", "top"));

        return [.. steps];
    }

    private static object Step(string element, string? tab, string title, string description,
                                string side = "right", string align = "center") =>
        tab is null
            ? new { element, popover = new { title, description, side, align } }
            : (object) new { element, tab, popover = new { title, description, side, align } };
}

// ─────────────────────────────────────────────
internal static class HomeWelcomeTourSteps
{
    public static object[] Build() =>
    [
        // ── Bienvenida ──
        Step("[data-tour='home-hero']",
            "Bienvenido a Piedra Azul",
            "Ofrecemos medicina alternativa y terapias especializadas para que viva activamente, sin dolor y con la mejor atención.",
            "bottom"),

        // ── Opción 1: Con cuenta ──
        Step("[data-tour='home-signin-card']",
            "Opción 1: Con Cuenta",
            "Cree una cuenta para acceder a su historial médico, revisar resultados y gestionar todas sus citas en un solo lugar seguro.",
            "left"),

        // ── Opción 2: Reserva rápida ──
        Step("[data-tour='home-quickbook-card']",
            "Opción 2: Reserva Rápida",
            "Si prefiere no crear una cuenta, reserve solo con su número de teléfono. Recibirá un SMS para confirmar su cita al instante.",
            "right"),

        // ── Fin ──
        Step("[data-tour='home-bottom-section']",
            "¡Está listo para comenzar!",
            "Si tienes dudas, puedes darle clic al botón para hacer un pequeño cuestionario.",
            "bottom"),
    ];

    private static object Step(string element, string title, string description, string side = "bottom", string align = "center") =>
        new { element, popover = new { title, description, side, align } };
}

// ─────────────────────────────────────────────
internal static class BookingTourSteps
{
    public static object[] Build() =>
    [
        // ── Paso 1: Selección de especialidad y médico ──
        // showButtons: solo "close" — el usuario DEBE elegir un médico para avanzar
        ActionStep("[data-tour='booking-specialist-section']",
            "Elige especialidad y médico",
            "Toca uno de los botones grandes para elegir tu especialidad. Luego selecciona el médico que prefieras de la lista. El tour avanzará automáticamente cuando elijas.",
            "bottom"),

        // ── Paso 2: Intermedio de carga ──
        // Se muestra mientras el scheduler carga los días disponibles.
        // El JS de waitForContentAndAdvance lo avanza automáticamente cuando desaparece el spinner.
        AutoStep("[data-tour='booking-scheduler-section']",
            "Un momento…",
            "Estamos buscando los horarios disponibles de este médico. En segundos podrás elegir tu cita.",
            "bottom"),

        // ── Paso 3: Selección de horario ──
        // showButtons: solo "close" — el usuario DEBE seleccionar día y hora para avanzar
        ActionStep("[data-tour='booking-scheduler-section']",
            "Elige un día y hora",
            "Selecciona el día que te convenga y luego el horario disponible. Cuando confirmes tu selección, el tour continuará solo.",
            "bottom"),

        // ── Paso 4: Confirmación ──
        Step("[data-tour='booking-confirmation']",
            "Revisa tus datos",
            "Confirma que el médico, la fecha y el horario sean los que deseas. Si todo está correcto, haz clic en «Siguiente».",
            "top"),

        // ── Paso 5: Solicitar cita ──
        Step("[data-tour='booking-next-btn']",
            "¡Solicita tu cita!",
            "Todo está listo. Haz clic en este botón para confirmar y registrar tu cita médica.",
            "top"),
    ];

    /// <summary>
    /// Paso normal con botones completos (Previous / Next / Close).
    /// </summary>
    private static object Step(string element, string title, string description, string side = "bottom", string align = "center") =>
        new { element, popover = new { title, description, side, align } };

    /// <summary>
    /// Paso de acción: solo muestra el botón de cerrar (X).
    /// El usuario DEBE completar la acción en pantalla; el tour avanza automáticamente.
    /// </summary>
    private static object ActionStep(string element, string title, string description, string side = "bottom", string align = "center") =>
        new { element, popover = new { title, description, side, align, showButtons = new[] { "close" } } };

    /// <summary>
    /// Paso automático: sin botones. El JS avanza el tour cuando el contenido termina de cargar.
    /// Se usa para pasos de "cargando…" que el usuario no puede saltar manualmente.
    /// </summary>
    private static object AutoStep(string element, string title, string description, string side = "bottom", string align = "center") =>
        new { element, popover = new { title, description, side, align, showButtons = Array.Empty<string>() } };
}
