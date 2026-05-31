using PiedraAzul.Client.Models.Booking;
using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Models.UserProfiles;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.Services.Utils;
using PiedraAzul.Client.States;
using PiedraAzul.Client.UI.Shared.Components.StepTag;
using Microsoft.AspNetCore.Components;
using PiedraAzul.Contracts.Enums;

namespace PiedraAzul.Client.UI.Features.Booking.Pages
{
    public partial class InstantBooking
    {
        [Inject] private PatientSearchService PatientSearchService { get; set; } = default!;

        /// <summary>Especialidad preseleccionada (viene de la consulta inteligente: ?specialty=OPTOMETRY).</summary>
        [Parameter]
        [SupplyParameterFromQuery(Name = "specialty")]
        public string? Specialty { get; set; }

        // ── Shared state (internal = accessible to InstantBookingEasyContent) ─
        internal DoctorType? _initialDoctorType;
        internal BookingModel Model = new();
        internal bool isSuccess = false;

        // ── Search / Verification ─────────────────────────────────────────────
        internal bool _showVerificationModal;
        internal bool _searching;
        internal GuestLookupResultGQL? _searchResult;
        internal string? _searchResultType;
        internal string? _searchError;

        // ── OTP FLUJO 1 (nuevo usuario) ───────────────────────────────────────
        internal bool _otpSent = false;
        internal bool _otpLoading = false;
        internal string? _otpError = null;

        // ── Channel Validation Modal (FLUJO 2 y FLUJO 3) ─────────────────────
        internal bool _showChannelValidationModal;
        internal bool _channelValidationLoading;
        internal string? _channelValidationError;

        // ── Modern-only state (Stepper, used only in the inline modern UI) ────
        private bool isLoading = false;
        internal string? _errorMessage;
        private Stepper<BookingModel> Stepper { get; set; }

        // ── Easy content notification ─────────────────────────────────────────
        /// <summary>
        /// Fired after every state change so InstantBookingEasyContent
        /// (which lives in a CascadingValue) can call StateHasChanged on itself.
        /// </summary>
        public event Action? OnEasyStateChanged;

        /// <summary>Calls StateHasChanged for the Modern UI AND fires OnEasyStateChanged.</summary>
        private void NotifyState() { StateHasChanged(); OnEasyStateChanged?.Invoke(); }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            _initialDoctorType = DoctorTypeGraphQLMapper.FromGraphQL(Specialty);

            var redirectUrl = _initialDoctorType.HasValue
                ? $"/medical-booking?specialty={Specialty}"
                : "/medical-booking";

            if (UserState.User != null)
                Navigation.NavigateTo(redirectUrl, forceLoad: false, replace: true);

            var response = await AuthService.GetCurrentUserAsync();
            if (response.IsSuccess)
                Navigation.NavigateTo(redirectUrl, forceLoad: false, replace: true);
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender && Stepper != null)
            {
                Stepper.OnNext = async () =>
                {
                    if (Stepper.CurrentStep == 0)
                    {
                        _searching = true;
                        _showVerificationModal = true;
                        _searchError = null;
                        _searchResult = null;
                        _searchResultType = null;
                        StateHasChanged();

                        await Task.Yield();
                        await SearchPatientByIdentificationAsync(Model.PatientIdentification!);

                        _searching = false;
                        StateHasChanged();
                        return false;
                    }
                    return true;
                };
            }
        }

        // ── Core search (shared) ──────────────────────────────────────────────

        private async Task SearchPatientByIdentificationAsync(string identification)
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
                var lookup = result.Value;
                _searchResult = lookup;
                Model.VerificationHash = lookup.VerificationHash;
                Model.HasPhoneAvailable = lookup.HasPhone;
                Model.HasEmailAvailable = lookup.HasEmail;
                Model.MaskedPhone = lookup.MaskedPhone;
                Model.MaskedEmail = lookup.MaskedEmail;

                _searchResultType = lookup.Type.ToUpperInvariant() switch
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
                Model.IsNewPatient = true;
            }
        }

        /// <summary>Entry point for Easy mode: opens the modal and runs the search.</summary>
        internal async Task EasySearchByIdentificationAsync(string cedula)
        {
            _searching = true;
            _showVerificationModal = true;
            _searchError = null;
            _searchResult = null;
            _searchResultType = null;
            Model.PatientIdentification = cedula.Trim();
            NotifyState();

            await Task.Yield();
            await SearchPatientByIdentificationAsync(Model.PatientIdentification);

            _searching = false;
            NotifyState();
        }

        // ── Modal callbacks (shared by Modern and Easy) ───────────────────────

        internal void OnRegisteredLogin()
        {
            _showVerificationModal = false;
            Navigation.NavigateTo($"/login?returnUrl=/instant-medical-booking");
        }

        internal void OnRegisteredGuestContinue()
        {
            _showVerificationModal = false;
            Model.IsRegisteredContinuingAsGuest = true;
            _showChannelValidationModal = true;
            NotifyState();
        }

        internal void OnGuestContinue()
        {
            _showVerificationModal = false;
            Model.IsRegisteredContinuingAsGuest = false;
            _showChannelValidationModal = true;
            NotifyState();
        }

        /// <summary>Modern mode only: also advances the Stepper.</summary>
        private void OnNewPatientContinue()
        {
            _showVerificationModal = false;
            Model.IsNewPatient = true;
            Stepper.GoToStep(1);
            NotifyState();
        }

        /// <summary>Easy mode: sets IsNewPatient; Easy content manages its own step.</summary>
        internal void OnNewPatientContinueEasy()
        {
            _showVerificationModal = false;
            Model.IsNewPatient = true;
            NotifyState();
        }

        internal async Task OnRetrySearch()
        {
            _searching = true;
            _searchError = null;
            _searchResultType = null;
            NotifyState();

            await SearchPatientByIdentificationAsync(Model.PatientIdentification!);

            _searching = false;
            NotifyState();
        }

        internal void OnVerificationModalClose()
        {
            _showVerificationModal = false;
            NotifyState();
        }

        // ── Channel Validation (shared) ───────────────────────────────────────

        internal async Task<bool> SendChannelValidationOtpAsync(string channel)
        {
            _channelValidationError = null;
            _channelValidationLoading = true;
            NotifyState();

            var result = await AppointmentService.SendGuestOtpByHashAsync(Model.VerificationHash!, channel);

            _channelValidationLoading = false;
            if (!result.IsSuccess)
                _channelValidationError = result.Error?.Message ?? "No se pudo enviar el código.";

            NotifyState();
            return result.IsSuccess;
        }

        internal async Task<GuestDataGQL?> VerifyChannelValidationOtpAsync(string channel, string code)
        {
            _channelValidationError = null;
            _channelValidationLoading = true;
            NotifyState();

            var result = await AppointmentService.VerifyGuestOtpByHashAsync(Model.VerificationHash!, code);

            _channelValidationLoading = false;
            NotifyState();

            if (!result.IsSuccess || result.Value is null) return null;
            return result.Value;
        }

        /// <summary>Shared core: persists OTP + updates model. Does NOT touch Stepper.</summary>
        private async Task<bool> ApplyChannelValidationConfirm((GuestDataGQL Data, string OtpCode) payload)
        {
            _channelValidationLoading = true;
            NotifyState();

            var (data, otpCode) = payload;
            var persistResult = await AppointmentService.VerifyGuestOtpByHashAsync(
                Model.VerificationHash!, otpCode, data.Name, data.Phone, data.Email);

            _channelValidationLoading = false;

            if (!persistResult.IsSuccess)
            {
                _channelValidationError = persistResult.Error?.Message ?? "Error al guardar los datos.";
                NotifyState();
                return false;
            }

            Model.PatientName = data.Name;
            Model.PatientPhone = data.Phone;
            Model.PatientEmail = string.IsNullOrWhiteSpace(data.Email) ? null : data.Email;
            Model.ChannelValidationVerified = true;
            Model.IsPreVerifiedGuest = true;
            Model.OtpVerified = true;
            _showChannelValidationModal = false;
            return true;
        }

        /// <summary>Modern mode: after confirm, also jumps Stepper to doctor step.</summary>
        private async Task OnChannelValidationConfirm((GuestDataGQL Data, string OtpCode) payload)
        {
            if (await ApplyChannelValidationConfirm(payload))
                Stepper.GoToStep(2);
            NotifyState();
        }

        /// <summary>Easy mode: after confirm, Easy content advances its own step.</summary>
        internal async Task OnChannelValidationConfirmEasy((GuestDataGQL Data, string OtpCode) payload)
        {
            await ApplyChannelValidationConfirm(payload);
            NotifyState();
        }

        internal void OnChannelValidationModalClose()
        {
            _showChannelValidationModal = false;
            _channelValidationError = null;
            Model.IsRegisteredContinuingAsGuest = false;
            NotifyState();
        }

        // ── Doctor & Slot (shared) ────────────────────────────────────────────

        internal void SelectedDoctor(DoctorModel? args)
        {
            if (args == null) return;
            Model.DoctorId = args.Id;
            Model.Doctor = args;
        }

        internal void SelectSlot(AppointmentSchedulerModel args)
        {
            if (args == null) return;
            args.Time = args.Time
                .Replace("a. m.", "AM").Replace("p. m.", "PM")
                .Replace("a.m.", "AM").Replace("p.m.", "PM")
                .Trim();
            Model.SlotId = args.SlotId;
            Model.DayOfYear = args.Date;
            Model.AppointmentSchedulerModel = args;
        }

        // ── OTP FLUJO 1 (Modern only, but internal in case needed) ────────────

        private async Task SendOtpAsync()
        {
            _otpError = null;
            _otpLoading = true;
            NotifyState();

            var result = await AppointmentService.SendGuestOtpAsync(
                Model.PatientPhone!,
                Model.OtpChannel == "email" ? Model.PatientEmail : null,
                Model.OtpChannel);

            _otpLoading = false;

            if (!result.IsSuccess)
                _otpError = result.Error?.Message ?? "No se pudo enviar el código. Intenta de nuevo.";
            else
            {
                Model.OtpSessionToken = result.Value;
                _otpSent = true;
            }

            NotifyState();
        }

        private async Task VerifyOtpAsync()
        {
            if (string.IsNullOrEmpty(Model.OtpCode) || Model.OtpCode.Length != 6) return;

            _otpError = null;
            _otpLoading = true;
            NotifyState();

            var result = await AppointmentService.VerifyGuestOtpAsync(Model.OtpSessionToken!, Model.OtpCode);

            _otpLoading = false;

            if (!result.IsSuccess)
                _otpError = result.Error?.Message ?? "Error al verificar el código.";
            else if (!result.Value)
                _otpError = "Código incorrecto. Intenta de nuevo.";
            else
                Model.OtpVerified = true;

            NotifyState();
        }

        private void ResetOtp()
        {
            _otpSent = false;
            _otpError = null;
            Model.OtpSessionToken = null;
            Model.OtpCode = null;
            Model.OtpVerified = false;
        }

        // ── Submit (shared) ───────────────────────────────────────────────────

        internal async Task HandlerSubmit()
        {
            if (!Model.OtpVerified && !Model.IsPreVerifiedGuest)
            {
                _errorMessage = "Debes verificar tu identidad antes de confirmar la cita.";
                NotifyState();
                return;
            }

            var extraInfo = !string.IsNullOrWhiteSpace(Model.PatientEmail) ? Model.PatientEmail : "";

            var result = await AppointmentService.BookGuestAppointmentAsync(new CreateAppointmentGqlInput(
                Guest: CreateContracts.CreateGuestPatientInput(
                    Model.PatientName!, Model.PatientPhone!, Model.PatientIdentification!, extraInfo, Model.PatientEmail),
                PatientUserId: null,
                DoctorId: Model.DoctorId,
                DoctorAvailabilitySlotId: Model.SlotId,
                Date: Model.DayOfYear.ToUniversalTime()
            ));

            if (!result.IsSuccess)
            {
                _errorMessage = "Ocurrió un error al crear la cita. Por favor, inténtelo de nuevo.";
                NotifyState();
                return;
            }

            isLoading = false;
            isSuccess = true;
            Stepper?.GoToStep(0);
            NotifyState();
        }
    }
}
