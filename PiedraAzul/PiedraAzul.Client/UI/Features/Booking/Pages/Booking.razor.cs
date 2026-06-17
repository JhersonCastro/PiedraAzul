using PiedraAzul.Client.Models.Booking;
using PiedraAzul.Client.Models.UserProfiles;
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
        private void SelectedDoctor(DoctorModel args)
        {
            if (args == null) return;
            Model.DoctorId = args.Id;
            Model.Doctor = args;
        }
        private async Task HandlerSubmit()
        {
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

            Console.WriteLine("meow");
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