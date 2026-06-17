using Microsoft.AspNetCore.Components;
using PiedraAzul.Client.Models.Booking;
using PiedraAzul.Client.Models.UserProfiles;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.Services.Utils;

namespace PiedraAzul.Client.UI.Features.Booking.Pages
{
    public partial class ManualBooking
    {
        private BookingModel Model { get; set; } = new();
        private List<PatientModel> SearchResult { get; set; } = new();
        private string? Search { get; set; }
        private bool _isNewPatient = false;

        private PatientModel? SelectedPatient;
        private async Task HandlerSearch()
        {
            var response = await PatientService.SearchPatientsAsync(Search ?? "", 10);
            SearchResult = response.Select(p => new PatientModel
            {
                Id = p.Id,
                PatientName = p.Name,
                PatientIdentification = p.Identification,
                PatientPhone = p.Phone,
                Type = p.Type == "GUEST" ? PatientTypeClient.Guest : PatientTypeClient.Registered
            }).ToList();

            await InvokeAsync(StateHasChanged);
        }
        private string GetPatientClass(bool isSelected)
        {
            var baseClass = "cursor-pointer rounded-xl border p-4 transition";

            if (isSelected)
            {
                return $"{baseClass} border-teal-600 bg-teal-50 ring-2 ring-teal-500";
            }

            return $"{baseClass} border-gray-200 bg-gray-50 hover:border-teal-400";
        }
        private void HandlerSelectDoctor(Models.UserProfiles.DoctorModel args)
        {
            if (args == null) return;

            Model.DoctorId = args.Id;
            Model.Doctor = args;
        }

        private void HandlerSelectAppointmentDate(AppointmentSchedulerModel args)
        {
            if (args == null) return;

            Model.AppointmentSchedulerModel = args;
            Model.DayOfYear = args.Date;
            Model.SlotId = args.SlotId;
        }

        private async Task HandlerChange()
        {
            Model.AppointmentSchedulerModel = null;
            Model.DayOfYear = default;
            Model.SlotId = null;
            Model.Doctor = null;
            Model.DoctorId = null;

            await InvokeAsync(StateHasChanged);
        }

        private async Task HandlerSubmit()
        {
            string? patientUserId = null;
            GuestPatientGqlInput? guestInput = null;

            if (SelectedPatient != null)
            {
                if (SelectedPatient.IsRegistered)
                    patientUserId = SelectedPatient.Id;
                else
                    guestInput = CreateContracts.CreateGuestPatientInput(
                        SelectedPatient.PatientName, SelectedPatient.PatientPhone,
                        SelectedPatient.PatientIdentification, "");
            }
            else if (Model.PatientName != null)
            {
                guestInput = CreateContracts.CreateGuestPatientInput(
                    Model.PatientName, Model.PatientPhone ?? "",
                    Model.PatientIdentification ?? "", "");
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
                Console.WriteLine(result.Error?.Message);
        }

        private void HandlerCancel()
        {
            Model = new();
        }
        private void SelectPatient(PatientModel patient)
        {
            SelectedPatient = patient;

            Model.PatientName = patient.PatientName;
            Model.PatientIdentification = patient.PatientIdentification;
            Model.PatientPhone = patient.PatientPhone;
        }

        private CancellationTokenSource? _cts;

        private async Task HandlerInputSearch(ChangeEventArgs args)
        {
            if (args.Value == null) return;

            var query = args.Value.ToString();

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            try
            {
                await Task.Delay(300, _cts.Token);

                if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                {
                    SearchResult = new List<PatientModel>();
                    return;
                }

                var response = await PatientService.SearchAutoCompletePatientsAsync(query, 10);

                SearchResult = response.Select(p => new PatientModel
                {
                    Id = p.Id,
                    PatientName = p.Name,
                    PatientIdentification = p.Identification,
                    PatientPhone = p.Phone,
                    Type = p.Type == "GUEST" ? PatientTypeClient.Guest : PatientTypeClient.Registered
                }).ToList();
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
}