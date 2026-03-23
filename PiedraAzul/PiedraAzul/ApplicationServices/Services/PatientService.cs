using Microsoft.EntityFrameworkCore;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;

namespace PiedraAzul.ApplicationServices.Services
{
    public interface IPatientService
    {
        Task<List<PatientProfile?>> GetPatientProfileByQueryAsync(string query);
        Task<List<PatientGuest?>> GetPatientGuestByQuery(string query);
        Task<PatientGuest?> GetPatientGuestById(string id);
        Task<PatientGuest> CreatePatientGuestAsync(PatientGuest patientGuest);  


        Task<PatientProfile?> GetPatientProfileByIdAsync(string id);

        Task<PatientProfile> CreatePatientProfileAsync(PatientProfile patientProfile);
    }
    public class PatientService(IDbContextFactory<AppDbContext> dbContextFactory) : IPatientService
    {
        public async Task<PatientGuest> CreatePatientGuestAsync(PatientGuest patientGuest)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();
            context.PatientGuests.Add(patientGuest);
            await context.SaveChangesAsync();
            return patientGuest;
        }

        public async Task<PatientProfile> CreatePatientProfileAsync(PatientProfile patientProfile)
        {
            using var context = dbContextFactory.CreateDbContext();
            context.Add(patientProfile);
            await context.SaveChangesAsync();
            return patientProfile;
        }

        public async Task<PatientGuest?> GetPatientGuestById(string id)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();

            return await context.PatientGuests.FindAsync(id);
        }

        public async Task<List<PatientGuest>?> GetPatientGuestByQuery(string query)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();
            var result = await context.PatientGuests
                                    .Where(pg => pg.PatientIdentification == query || 
                                    pg.PatientName == query || pg.PatientPhone == query)
                                    .ToListAsync();
            return result;
        }

        public Task<PatientProfile?> GetPatientProfileByIdAsync(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<PatientProfile>?> GetPatientProfileByQueryAsync(string query)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();

            var result = await context.PatientProfiles.Include(pp => pp.User)
                            .Where(pp => pp.User.Email == query || pp.User.PhoneNumber == query)
                            .ToListAsync();
            return result;
        }
    }
}
