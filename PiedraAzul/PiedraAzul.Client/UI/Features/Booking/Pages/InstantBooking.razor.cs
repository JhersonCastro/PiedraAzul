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

        private DoctorType? _initialDoctorType;

        BookingModel Model = new();
        bool isLoading = false;
        bool isSuccess = false;

        Stepper<BookingModel> Stepper { get; set; }

        string? _errorMessage;

        // ── Patient Search / Verification Modal ───────────────────────────
        private bool _showVerificationModal;
        private bool _searching;
        private GuestLookupResultGQL? _searchResult;
        private string? _searchResultType;
        private string? _searchError;

        // ── OTP final (FLUJO 1 - nuevo usuario) ───────────────────────────
        bool _otpSent = false;
        bool _otpLoading = false;
        string? _otpError = null;

        // ── Channel Validation Modal (FLUJO 2 y FLUJO 3) ──────────────────
        private bool _showChannelValidationModal;
        private bool _channelValidationLoading;
        private string? _channelValidationError;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            _initialDoctorType = DoctorTypeGraphQLMapper.FromGraphQL(Specialty);

            // Si el usuario está autenticado usa el flujo con cuenta, conservando la especialidad.
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

        // ── Patient Search ────────────────────────────────────────────────

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

                // Guardar datos enmascarados y canales disponibles
                Model.VerificationHash = lookup.VerificationHash;
                Model.HasPhoneAvailable = lookup.HasPhone;
                Model.HasEmailAvailable = lookup.HasEmail;
                Model.MaskedPhone = lookup.MaskedPhone;
                Model.MaskedEmail = lookup.MaskedEmail;

                _searchResultType = lookup.Type.ToUpperInvariant() switch
                {
                    "REGISTERED" => "REGISTERED",
                    "GUEST" => "GUEST",
                    "NOCONTACT" or "NO_CONTACT" => "NO_CONTACT",
                    _ => "NOT_FOUND"
                };
            }
            else
            {
                _searchResultType = "NOT_FOUND";
                Model.IsNewPatient = true;
            }
        }

        // ── Modal Callbacks (PatientVerificationModal) ────────────────────

        private void OnRegisteredLogin()
        {
            _showVerificationModal = false;
            Navigation.NavigateTo($"/login?returnUrl=/instant-medical-booking");
        }

        /// <summary>FLUJO 2: Usuario registrado continúa como invitado → abrir modal de validación</summary>
        private void OnRegisteredGuestContinue()
        {
            _showVerificationModal = false;
            Model.IsRegisteredContinuingAsGuest = true;
            _showChannelValidationModal = true;
            StateHasChanged();
        }

        /// <summary>FLUJO 3: Guest existente → abrir modal de validación</summary>
        private void OnGuestContinue()
        {
            _showVerificationModal = false;
            Model.IsRegisteredContinuingAsGuest = false;
            _showChannelValidationModal = true;
            StateHasChanged();
        }

        /// <summary>FLUJO 1: Nuevo paciente</summary>
        private void OnNewPatientContinue()
        {
            _showVerificationModal = false;
            Model.IsNewPatient = true;
            Stepper.GoToStep(1);
            StateHasChanged();
        }

        private async Task OnRetrySearch()
        {
            _searching = true;
            _searchError = null;
            _searchResultType = null;
            StateHasChanged();

            await SearchPatientByIdentificationAsync(Model.PatientIdentification!);

            _searching = false;
            StateHasChanged();
        }

        private void OnVerificationModalClose()
        {
            _showVerificationModal = false;
        }

        // ── Channel Validation Modal Callbacks (FLUJO 2 y FLUJO 3) ────────

        private async Task SendChannelValidationOtpAsync(string channel)
        {
            _channelValidationError = null;
            _channelValidationLoading = true;
            StateHasChanged();

            var result = await AppointmentService.SendGuestOtpByHashAsync(
                Model.VerificationHash!, channel);

            _channelValidationLoading = false;

            if (!result.IsSuccess)
                _channelValidationError = result.Error?.Message ?? "No se pudo enviar el código.";

            StateHasChanged();
        }

        private async Task<GuestDataGQL?> VerifyChannelValidationOtpAsync(string channel, string code)
        {
            _channelValidationError = null;
            _channelValidationLoading = true;
            StateHasChanged();

            // Solo verificar el OTP (sin actualizar datos aún - usuario verá los datos primero)
            var result = await AppointmentService.VerifyGuestOtpByHashAsync(
                Model.VerificationHash!, code);

            _channelValidationLoading = false;
            StateHasChanged();

            if (!result.IsSuccess || result.Value is null)
                return null;

            return result.Value;
        }

        /// <summary>Callback final del modal: usuario confirmó/editó sus datos.</summary>
        private async Task OnChannelValidationConfirm((GuestDataGQL Data, string OtpCode) payload)
        {
            _channelValidationLoading = true;
            StateHasChanged();

            var (data, otpCode) = payload;

            // Persistir los datos editados en BD volviendo a llamar verifyGuestOtpByHash con los datos.
            // La sesión ya está verificada → solo actualizará datos en BD (guest o sesión según tipo).
            var persistResult = await AppointmentService.VerifyGuestOtpByHashAsync(
                Model.VerificationHash!, otpCode, data.Name, data.Phone, data.Email);

            _channelValidationLoading = false;

            if (!persistResult.IsSuccess)
            {
                _channelValidationError = persistResult.Error?.Message ?? "Error al guardar los datos.";
                StateHasChanged();
                return;
            }

            // Aplicar datos al modelo local
            Model.PatientName = data.Name;
            Model.PatientPhone = data.Phone;
            Model.PatientEmail = string.IsNullOrWhiteSpace(data.Email) ? null : data.Email;
            Model.ChannelValidationVerified = true;
            Model.IsPreVerifiedGuest = true;
            Model.OtpVerified = true;  // Marcar verificado para que pase el submit final

            _showChannelValidationModal = false;

            // Saltar al step de selección de doctor (índice 2: 0=ID, 1=Datos, 2=Doctor)
            Stepper.GoToStep(2);
            StateHasChanged();
        }

        private void OnChannelValidationModalClose()
        {
            _showChannelValidationModal = false;
            _channelValidationError = null;
            Model.IsRegisteredContinuingAsGuest = false;
            StateHasChanged();
        }

        // ── Doctor & Slot ─────────────────────────────────────────────────

        private void SelectedDoctor(DoctorModel? args)
        {
            if (args == null) return;
            Model.DoctorId = args.Id;
            Model.Doctor = args;
        }

        private void SelectSlot(AppointmentSchedulerModel args)
        {
            if (args == null) return;

            args.Time = args.Time.Replace("a. m.", "AM")
                                 .Replace("p. m.", "PM")
                                 .Replace("a.m.", "AM")
                                 .Replace("p.m.", "PM")
                                 .Trim();

            Model.SlotId = args.SlotId;
            Model.DayOfYear = args.Date;
            Model.AppointmentSchedulerModel = args;
        }

        // ── OTP final (FLUJO 1 - solo nuevo usuario) ──────────────────────

        private async Task SendOtpAsync()
        {
            _otpError = null;
            _otpLoading = true;
            StateHasChanged();

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

            StateHasChanged();
        }

        private async Task VerifyOtpAsync()
        {
            if (string.IsNullOrEmpty(Model.OtpCode) || Model.OtpCode.Length != 6) return;

            _otpError = null;
            _otpLoading = true;
            StateHasChanged();

            var result = await AppointmentService.VerifyGuestOtpAsync(
                Model.OtpSessionToken!, Model.OtpCode);

            _otpLoading = false;

            if (!result.IsSuccess)
                _otpError = result.Error?.Message ?? "Error al verificar el código.";
            else if (!result.Value)
                _otpError = "Código incorrecto. Intenta de nuevo.";
            else
                Model.OtpVerified = true;

            StateHasChanged();
        }

        private void ResetOtp()
        {
            _otpSent = false;
            _otpError = null;
            Model.OtpSessionToken = null;
            Model.OtpCode = null;
            Model.OtpVerified = false;
        }

        // ── Submit ────────────────────────────────────────────────────────

        private async Task HandlerSubmit()
        {
            if (!Model.OtpVerified && !Model.IsPreVerifiedGuest)
            {
                _errorMessage = "Debes verificar tu identidad antes de confirmar la cita.";
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
                return;
            }

            isLoading = false;
            isSuccess = true;
            Stepper?.GoToStep(0);
        }
    }
}
