using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using PiedraAzul.Client.Models.Booking;
using PiedraAzul.Client.Models.UserProfiles;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.Services.Utils;
using System.Security.Claims;

namespace PiedraAzul.Client.UI.Features.Booking.Pages
{
    public partial class ManualBooking
    {
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] private PiedraAzul.Client.Services.DoctorPortalState DoctorPortalState { get; set; } = default!;

        /// <summary>
        /// Paciente pre-seleccionado desde el portal del doctor (DoctorPortalState).
        /// Si viene con paciente, se auto-selecciona al inicializar.
        /// </summary>
        [Parameter] public PatientModel? PreselectedPatient { get; set; }

        private BookingModel Model { get; set; } = new();
        private PatientModel? SelectedPatient;

        // ─── Role ────────────────────────────────────────────────
        private bool _isDoctor;
        private bool _isAdmin;
        private string _currentUserId = "";
        private string _currentUserName = "";

        // ─── Patient search modal ────────────────────────────────
        private bool _patientModalOpen;
        private string? _errorMsg;
        private bool _isSubmitting;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            _isDoctor = user.IsInRole("Doctor");
            _isAdmin  = user.IsInRole("Admin");
            _currentUserId   = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            _currentUserName = user.FindFirst(ClaimTypes.Name)?.Value ?? user.Identity?.Name ?? "";

            // Si es doctor, pre-rellenar el doctorId con el usuario actual
            if (_isDoctor)
            {
                Model.DoctorId = _currentUserId;
            }

            // Auto-seleccionar paciente que viene del portal del doctor
            var patientToPreselect = PreselectedPatient ?? DoctorPortalState.PreselectedPatient;
            if (patientToPreselect is not null)
            {
                OnPatientSelected(patientToPreselect);
                DoctorPortalState.PreselectedPatient = null; // limpiar para próxima vez
            }
        }

        // ─── Doctor selection (solo Admin) ───────────────────────
        private void HandlerSelectDoctor(Models.UserProfiles.DoctorModel args)
        {
            if (args == null) return;
            Model.DoctorId = args.Id;
            Model.Doctor   = args;
        }

        private async Task HandlerChange()
        {
            Model.AppointmentSchedulerModel = null;
            Model.DayOfYear = default;
            Model.SlotId    = null;
            Model.Doctor    = null;
            if (!_isDoctor)
                Model.DoctorId = null;

            await InvokeAsync(StateHasChanged);
        }

        // ─── Patient search modal ────────────────────────────────
        private void OpenPatientModal() => _patientModalOpen = true;

        private void OnPatientSelected(PatientModel patient)
        {
            SelectedPatient = patient;
            Model.PatientName           = patient.PatientName;
            Model.PatientIdentification = patient.PatientIdentification;
            Model.PatientPhone          = patient.PatientPhone;
            _patientModalOpen           = false;
            StateHasChanged();
        }

        private void OnPatientModalCancel()
        {
            _patientModalOpen = false;
            StateHasChanged();
        }

        private void ClearPatient()
        {
            SelectedPatient             = null;
            Model.PatientName           = null;
            Model.PatientIdentification = null;
            Model.PatientPhone          = null;
        }

        // ─── Scheduler ──────────────────────────────────────────
        private void HandlerSelectAppointmentDate(AppointmentSchedulerModel args)
        {
            if (args == null) return;
            Model.AppointmentSchedulerModel = args;
            Model.DayOfYear = args.Date;
            Model.SlotId    = args.SlotId;
        }

        // ─── Submit ──────────────────────────────────────────────
        private async Task HandlerSubmit()
        {
            _errorMsg = null;

            if (string.IsNullOrWhiteSpace(Model.DoctorId))
            {
                _errorMsg = "Debes seleccionar un doctor";
                return;
            }

            if (SelectedPatient == null && string.IsNullOrWhiteSpace(Model.PatientName))
            {
                _errorMsg = "Debes seleccionar o registrar un paciente";
                return;
            }

            if (string.IsNullOrWhiteSpace(Model.SlotId))
            {
                _errorMsg = "Debes seleccionar una fecha y hora disponible";
                return;
            }

            _isSubmitting = true;

            try
            {
                string? patientUserId = null;
                GuestPatientGqlInput? guestInput = null;

                if (SelectedPatient != null)
                {
                    if (SelectedPatient.IsRegistered)
                        patientUserId = SelectedPatient.Id;
                    else
                        guestInput = CreateContracts.CreateGuestPatientInput(
                            SelectedPatient.PatientName,
                            SelectedPatient.PatientPhone,
                            SelectedPatient.PatientIdentification,
                            "");
                }
                else if (Model.PatientName != null)
                {
                    guestInput = CreateContracts.CreateGuestPatientInput(
                        Model.PatientName,
                        Model.PatientPhone ?? "",
                        Model.PatientIdentification ?? "",
                        "");
                }

                var input = new CreateAppointmentGqlInput(
                    DoctorId: Model.DoctorId!,
                    DoctorAvailabilitySlotId: Model.SlotId!,
                    Date: Model.DayOfYear,
                    PatientUserId: patientUserId,
                    Guest: guestInput
                );

                var result = await AppointmentService.CreateAppointment(input);

                if (!result.IsSuccess)
                    _errorMsg = result.Error?.Message ?? "Error al guardar la cita";
                else
                    HandlerCancel();
            }
            finally
            {
                _isSubmitting = false;
            }
        }

        private void HandlerCancel()
        {
            Model           = new();
            SelectedPatient = null;
            _errorMsg       = null;

            if (_isDoctor)
                Model.DoctorId = _currentUserId;
        }
    }
}
