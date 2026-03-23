using PiedraAzul.Data.Models;
using Riok.Mapperly.Abstractions;
using Shared.Grpc;
namespace PiedraAzul.ApplicationServices.Mapping
{
    [Mapper]
    public partial class PatientMapper
    {
        public partial PatientGuest ToEntity(CreateAppointmentRequest request);

    }
}
