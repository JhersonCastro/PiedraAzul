using PiedraAzul.Client.Models.Booking;
using PiedraAzul.Client.Models.UserProfiles;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.Services.Utils;
using PiedraAzul.Client.States;
using PiedraAzul.Client.UI.Shared.Components.StepTag;

namespace PiedraAzul.Client.UI.Features.Booking.Pages
{
    public partial class InstantBooking
    {

        BookingModel Model = new();
        bool isLoading = false;
        bool isSuccess = false;

        Stepper<BookingModel> Stepper { get; set; }

        string? _errorMessage;

        // ── OTP ──────────────────────────────────────────────────────────
        bool _otpSent = false;
        bool _otpLoading = false;
        string? _otpError = null;
        // ─────────────────────────────────────────────────────────────────

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (UserState.User != null)
                Navigation.NavigateTo("/medical-booking", forceLoad: false, replace: true);

            var response = await AuthService.GetCurrentUserAsync();
            if (response.IsSuccess)
                Navigation.NavigateTo("/medical-booking", forceLoad: false, replace: true);
        }

        private void SelectedDoctor(DoctorModel? args)
        {
            if (args == null) return;
            Model.DoctorId = args.Id;
            Model.Doctor = args;
        }

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
            {
                _otpError = result.Error?.Message ?? "No se pudo enviar el código. Intenta de nuevo.";
            }
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
            {
                _otpError = result.Error?.Message ?? "Error al verificar el código.";
            }
            else if (!result.Value)
            {
                _otpError = "Código incorrecto. Intenta de nuevo.";
            }
            else
            {
                Model.OtpVerified = true;
            }

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

        private async Task HandlerSubmit()
        {
            if (!Model.OtpVerified)
            {
                _errorMessage = "Debes verificar tu identidad antes de confirmar la cita.";
                return;
            }

            // Pasamos el email del huésped en ExtraInfo si eligió ese canal
            var extraInfo = Model.OtpChannel == "email" ? Model.PatientEmail ?? "" : "";

            var result = await AppointmentService.CreateAppointment(new CreateAppointmentGqlInput(
                Guest: CreateContracts.CreateGuestPatientInput(
                    Model.PatientName!, Model.PatientPhone!, Model.PatientIdentification!, extraInfo),
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
    }
}