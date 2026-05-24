using Microsoft.JSInterop;
using PiedraAzul.Client.Models.Booking;
using PiedraAzul.Client.Models.UserProfiles;
using PiedraAzul.Client.Services;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.States;
using PiedraAzul.Client.UI.Shared.Components.StepTag;

namespace PiedraAzul.Client.UI.Features.Booking.Pages
{
    public partial class Booking
    {
        private string _patientId;
        BookingModel Model = new();
        string _errorMessage;

        public Stepper<BookingModel> Stepper { get; set; }

        bool isLoading = false;
        bool isSuccess = false;

        // ── Tour ──────────────────────────────────────────────
        private DotNetObjectReference<Booking>? _tourRef;
        private bool _tourActive = false;

        /// <summary>
        /// Tracks how many asistivo tour auto-advances have fired,
        /// preventing double-advancement if the user repeats an action.
        /// 0 = before doctor selected, 1 = before slot selected.
        /// </summary>
        private int _tourStepsDone = 0;
        // ──────────────────────────────────────────────────────

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (UserState.User != null)
            {
                _patientId = UserState.User.Id;
                return;
            }

            var response = await AuthService.GetCurrentUserAsync();

            if (!response.IsSuccess)
            {
                Navigation.NavigateTo("/instant-medical-booking", forceLoad: false, replace: true);
                return;
            }

            UserState.User = response.Value!;
            _patientId = response.Value!.Id;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var shouldShow = await DriverTourService.ShouldShowTourAsync(TourIds.BookingTour);
                if (shouldShow)
                    await StartTourAsync();
            }
        }

        // ── Tour methods ───────────────────────────────────────

        private async Task StartTourAsync()
        {
            _tourRef ??= DotNetObjectReference.Create(this);
            _tourActive = true;
            _tourStepsDone = 0;
            await DriverTourService.StartTourAsync(TourIds.BookingTour, dotnetRef: _tourRef);
        }

        [JSInvokable]
        public Task TourNavigateToTab(string tab) => Task.CompletedTask;

        public void Dispose() => _tourRef?.Dispose();

        // ── Booking callbacks ──────────────────────────────────

        private async Task SelectedDoctor(DoctorModel args)
        {
            if (args == null) return;
            Model.DoctorId = args.Id;
            Model.Doctor = args;

            // Tour asistivo: first doctor selection → advance driver through the
            // intermediate "loading…" step, then auto-advance once scheduler content is ready.
            if (_tourActive && _tourStepsDone == 0)
            {
                _tourStepsDone = 1;
                StateHasChanged();
                await Stepper.Next();
                // waitForContentAndAdvance hace DOS avances del driver:
                //   1. Inmediato → muestra el paso "Un momento… buscando horarios"
                //   2. Cuando booking-scheduler-section ya no tiene spinner → muestra el paso de selección
                await JS.InvokeVoidAsync("DriverTour.waitForContentAndAdvance", "[data-tour='booking-scheduler-section']");
            }
        }

        private async Task HandlerSubmit()
        {
            // Clean up the tour before switching to the success view
            if (_tourActive)
            {
                _tourActive = false;
                await DriverTourService.DestroyAsync();
            }

            var result = await AppointmentService.CreateAppointment(new CreateAppointmentGqlInput(
                Guest: null,
                PatientUserId: _patientId,
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
            Stepper.GoToStep(0);

            _ = OfflineCache.SyncAsync();
        }

        private async Task SelectSlot(AppointmentSchedulerModel args)
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

            // Tour asistivo: first slot selection → advance driver to confirmation step
            // and auto-advance the stepper to show the confirmation view
            if (_tourActive && _tourStepsDone == 1)
            {
                _tourStepsDone = 2;
                StateHasChanged();
                await Stepper.Next();
                // Esperar a que booking-confirmation esté en el DOM antes de avanzar
                await JS.InvokeVoidAsync("DriverTour.advanceTour", "[data-tour='booking-confirmation']");
            }
        }
    }
}
