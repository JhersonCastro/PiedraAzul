using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PiedraAzul.Client.Models.Booking;
using PiedraAzul.Client.Models.UserProfiles;
using PiedraAzul.Client.Services;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.States;
using PiedraAzul.Client.UI.Shared.Components.StepTag;
using PiedraAzul.Contracts.Enums;

namespace PiedraAzul.Client.UI.Features.Booking.Pages
{
    public partial class Booking
    {
        internal string _patientId;
        internal BookingModel Model = new();
        string _errorMessage;

        /// <summary>Especialidad preseleccionada (viene de la consulta inteligente: ?specialty=OPTOMETRY).</summary>
        [Parameter]
        [SupplyParameterFromQuery(Name = "specialty")]
        public string? Specialty { get; set; }

        internal DoctorType? _initialDoctorType;

        public Stepper<BookingModel> Stepper { get; set; }

        bool isLoading = false;
        internal bool isSuccess = false;
        internal bool _isSubmitting = false;

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

        // ── Easy content notification ─────────────────────────────────────────
        /// <summary>
        /// Fired after every state change so BookingEasyContent
        /// (which lives in a CascadingValue) can call StateHasChanged on itself.
        /// </summary>
        public event Action? OnEasyStateChanged;

        /// <summary>Calls StateHasChanged for the Modern UI AND fires OnEasyStateChanged.</summary>
        private void NotifyState() { StateHasChanged(); OnEasyStateChanged?.Invoke(); }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            _initialDoctorType = DoctorTypeGraphQLMapper.FromGraphQL(Specialty);

            if (UserState.User != null)
            {
                _patientId = UserState.User.Id;
                return;
            }

            var redirectUrl = _initialDoctorType.HasValue
                ? $"/instant-medical-booking?specialty={Specialty}"
                : "/instant-medical-booking";

            var response = await AuthService.GetCurrentUserAsync();

            if (!response.IsSuccess)
            {
                Navigation.NavigateTo(redirectUrl, forceLoad: false, replace: true);
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

        internal async Task SelectedDoctor(DoctorModel args)
        {
            if (args == null) return;
            Model.DoctorId = args.Id;
            Model.Doctor = args;

            // Tour asistivo: first doctor selection → advance driver through the
            // intermediate "loading…" step, then auto-advance once scheduler content is ready.
            if (_tourActive && _tourStepsDone == 0)
            {
                _tourStepsDone = 1;
                NotifyState();
                await Stepper.Next();
                // waitForContentAndAdvance hace DOS avances del driver:
                //   1. Inmediato → muestra el paso "Un momento… buscando horarios"
                //   2. Cuando booking-scheduler-section ya no tiene spinner → muestra el paso de selección
                await JS.InvokeVoidAsync("DriverTour.waitForContentAndAdvance", "[data-tour='booking-scheduler-section']");
            }
            else
            {
                NotifyState();
            }
        }

        internal async Task HandlerSubmit()
        {
            // Clean up the tour before switching to the success view
            if (_tourActive)
            {
                _tourActive = false;
                await DriverTourService.DestroyAsync();
            }

            _isSubmitting = true;
            NotifyState();

            var result = await AppointmentService.CreateAppointment(new CreateAppointmentGqlInput(
                Guest: null,
                PatientUserId: _patientId,
                DoctorId: Model.DoctorId,
                DoctorAvailabilitySlotId: Model.SlotId,
                Date: Model.DayOfYear.ToUniversalTime()
            ));

            _isSubmitting = false;

            if (!result.IsSuccess)
            {
                _errorMessage = "Ocurrió un error al crear la cita. Por favor, inténtelo de nuevo.";
                NotifyState();
                return;
            }

            isLoading = false;
            isSuccess = true;
            Stepper?.GoToStep(0);

            _ = OfflineCache.SyncAsync();
            NotifyState();
        }

        internal async Task SelectSlot(AppointmentSchedulerModel args)
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
                NotifyState();
                await Stepper.Next();
                // Esperar a que booking-confirmation esté en el DOM antes de avanzar
                await JS.InvokeVoidAsync("DriverTour.advanceTour", "[data-tour='booking-confirmation']");
            }
            else
            {
                NotifyState();
            }
        }
    }
}
