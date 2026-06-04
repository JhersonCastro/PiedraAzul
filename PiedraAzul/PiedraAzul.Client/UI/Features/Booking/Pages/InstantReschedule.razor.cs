using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.States;

namespace PiedraAzul.Client.UI.Features.Booking.Pages
{
    public partial class InstantReschedule
    {
        [Inject] private GraphQLAppointmentService AppointmentService { get; set; } = default!;
        [Inject] private PatientSearchService PatientSearchService { get; set; } = default!;
        [Inject] private UIModeState UIModeState { get; set; } = default!;

        internal enum Stage { Identification, Appointments }

        internal Stage _stage = Stage.Identification;
        internal string? _identification;
        internal string? _error;
        internal bool _isSuccess;

        // Lookup / verification
        internal bool _searching;
        internal bool _showVerificationModal;
        internal GuestLookupResultGQL? _searchResult;
        internal string? _searchResultType;
        internal string? _searchError;
        internal string? _verificationHash;

        // Channel OTP modal — canal activo (puede ser el del registrado o el del invitado)
        internal bool _showChannelModal;
        internal bool _channelLoading;
        internal bool _activeHasPhone;
        internal bool _activeHasEmail;
        internal string? _activeMaskedPhone;
        internal string? _activeMaskedEmail;

        // Contexto invitado: true cuando un registrado verifica el 2º OTP para ver
        // y reagendar las citas de su cuenta invitada (misma cédula).
        internal bool _inGuestContext;

        // Appointments
        internal bool _loadingAppointments;
        internal List<AppointmentGQL> _appointments = [];

        // Reschedule
        internal bool _showRescheduleModal;
        internal bool _rescheduleLoading;
        internal AppointmentGQL? _selectedAppt;

        // ── Easy content notification ──────────────────────────────────────────
        /// <summary>
        /// Fired after every state change so InstantRescheduleEasyContent
        /// (which lives in a CascadingValue) can call StateHasChanged on itself.
        /// </summary>
        public event Action? OnEasyStateChanged;

        /// <summary>Calls StateHasChanged for the Modern UI AND fires OnEasyStateChanged.</summary>
        internal void NotifyState() { StateHasChanged(); OnEasyStateChanged?.Invoke(); }

        // ─── ID lookup ────────────────────────────────────────────────────────────

        internal async Task OnIdKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !_searching && !string.IsNullOrWhiteSpace(_identification))
                await StartSearchAsync();
        }

        internal async Task StartSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(_identification) || _searching) return;

            _searching = true;
            _showVerificationModal = false;
            _searchError = null;
            _searchResult = null;
            _searchResultType = null;
            _inGuestContext = false;
            NotifyState();

            await DoSearchAsync(_identification);

            _searching = false;
            _showVerificationModal = true;
            NotifyState();
        }

        private async Task DoSearchAsync(string identification)
        {
            var result = await PatientSearchService.LookupByIdentificationAsync(identification);

            if (!result.IsSuccess)
            {
                _searchError = result.Error?.Message ?? "Error de conexión";
                _searchResultType = "ERROR";
                return;
            }

            if (result.Value is not null)
            {
                _searchResult = result.Value;
                _verificationHash = result.Value.VerificationHash;
                _searchResultType = result.Value.Type.ToUpperInvariant() switch
                {
                    "REGISTERED" => "REGISTERED",
                    "GUEST"      => "GUEST",
                    "NOCONTACT" or "NO_CONTACT" => "NO_CONTACT",
                    _            => "NOT_FOUND"
                };
            }
            else
            {
                _searchResultType = "NOT_FOUND";
            }
        }

        // ─── Verification modal callbacks ─────────────────────────────────────────

        internal void OnRegisteredLogin()
        {
            _showVerificationModal = false;
            _error = "Para reagendar tu cita, inicia sesión en tu cuenta y ve a 'Mis citas'.";
            _stage = Stage.Identification;
            NotifyState();
        }

        internal void OnGuestContinue()
        {
            _showVerificationModal = false;
            // El OTP se envía al canal que devolvió el lookup (registrado o guest puro).
            _activeHasPhone = _searchResult?.HasPhone ?? false;
            _activeHasEmail = _searchResult?.HasEmail ?? false;
            _activeMaskedPhone = _searchResult?.MaskedPhone;
            _activeMaskedEmail = _searchResult?.MaskedEmail;
            _inGuestContext = false;
            _showChannelModal = true;
            NotifyState();
        }

        /// <summary>
        /// Banner (solo registrados con citas de invitado): cambia el contexto a la cuenta
        /// invitada — reapunta el hash y los canales al del invitado — y abre el 2º OTP.
        /// </summary>
        internal void OnVerifyGuestAppointments()
        {
            if (string.IsNullOrEmpty(_searchResult?.GuestVerificationHash)) return;

            _verificationHash = _searchResult.GuestVerificationHash;
            _activeHasPhone = _searchResult.GuestHasPhone;
            _activeHasEmail = _searchResult.GuestHasEmail;
            _activeMaskedPhone = _searchResult.GuestMaskedPhone;
            _activeMaskedEmail = _searchResult.GuestMaskedEmail;
            _inGuestContext = true;
            _error = null;
            _showChannelModal = true;
            NotifyState();
        }

        internal void OnNotFound()
        {
            _showVerificationModal = false;
            _error = "No encontramos citas asociadas a ese número de identificación.";
            NotifyState();
        }

        internal async Task OnRetrySearch()
        {
            if (string.IsNullOrWhiteSpace(_identification)) return;

            _searching = true;
            _showVerificationModal = false;
            _searchError = null;
            _searchResultType = null;
            NotifyState();

            await DoSearchAsync(_identification);

            _searching = false;
            _showVerificationModal = true;
            NotifyState();
        }

        internal void OnVerificationModalClose()
        {
            _showVerificationModal = false;
            NotifyState();
        }

        // ─── Channel OTP modal callbacks ──────────────────────────────────────────

        internal async Task<bool> SendOtpByHashAsync(string channel)
        {
            _channelLoading = true;
            NotifyState();

            var result = await AppointmentService.SendGuestOtpByHashAsync(_verificationHash!, channel);

            _channelLoading = false;
            NotifyState();

            return result.IsSuccess;
        }

        internal async Task<GuestDataGQL?> VerifyOtpByHashAsync(string channel, string code)
        {
            _channelLoading = true;
            NotifyState();

            var result = await AppointmentService.VerifyGuestOtpByHashAsync(_verificationHash!, code);

            _channelLoading = false;
            NotifyState();

            return result.IsSuccess ? result.Value : null;
        }

        internal async Task OnChannelConfirm((GuestDataGQL Data, string OtpCode) payload)
        {
            _channelLoading = true;
            NotifyState();

            await AppointmentService.VerifyGuestOtpByHashAsync(
                _verificationHash!, payload.OtpCode, payload.Data.Name, payload.Data.Phone, payload.Data.Email);

            _channelLoading = false;
            _showChannelModal = false;

            // Mostrar de inmediato el stage de citas con su spinner, para que no quede
            // un vacío sin feedback entre el cierre del modal y la carga de citas.
            _stage = Stage.Appointments;
            _loadingAppointments = true;
            _appointments = [];
            NotifyState();

            await LoadGuestAppointmentsAsync();
        }

        internal void OnChannelModalClose()
        {
            _showChannelModal = false;
            NotifyState();
        }

        // ─── Appointments ─────────────────────────────────────────────────────────

        internal async Task LoadGuestAppointmentsAsync()
        {
            _loadingAppointments = true;
            _appointments = [];
            NotifyState();

            var result = await AppointmentService.GetGuestUpcomingAppointmentsAsync(_verificationHash!);

            _loadingAppointments = false;

            if (!result.IsSuccess)
            {
                _error = "No se pudieron cargar las citas. Intenta de nuevo.";
                NotifyState();
                return;
            }

            _appointments = result.Value ?? [];
            NotifyState();
        }

        // ─── Reschedule ───────────────────────────────────────────────────────────

        internal void OpenRescheduleModal(AppointmentGQL appt)
        {
            _selectedAppt = appt;
            _showRescheduleModal = true;
            _error = null;
            NotifyState();
        }

        internal async Task HandleRescheduleConfirm((string DoctorId, string SlotId, DateTime Date) selection)
        {
            if (_selectedAppt is null) return;

            _rescheduleLoading = true;
            NotifyState();

            var result = await AppointmentService.RescheduleAppointmentByHashAsync(
                _verificationHash!,
                Guid.Parse(_selectedAppt.Id),
                Guid.Parse(selection.SlotId),
                selection.Date.ToUniversalTime());

            _rescheduleLoading = false;

            if (!result.IsSuccess)
            {
                _error = result.Error?.Message ?? "No se pudo reagendar la cita. Intenta de nuevo.";
                _showRescheduleModal = false;
                _selectedAppt = null;
                NotifyState();
                return;
            }

            _showRescheduleModal = false;
            _selectedAppt = null;
            _isSuccess = true;
            NotifyState();
        }

        internal void ResetAll()
        {
            _isSuccess = false;
            _stage = Stage.Identification;
            _identification = null;
            _verificationHash = null;
            _searchResult = null;
            _appointments = [];
            _error = null;
            _inGuestContext = false;
            NotifyState();
        }
    }
}
