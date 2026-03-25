using Microsoft.EntityFrameworkCore;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;
using PiedraAzul.Shared.Enums;

namespace PiedraAzul.ApplicationServices.Services
{
    public interface IDoctorService
    {
        Task<List<DoctorProfile>> GetAllDoctorsAsync();
        Task<List<DoctorProfile>> GetDoctorByTypeAsync(DoctorType type);
        Task<DoctorProfile> GetDoctorByIdAsync(Guid doctorId);
    }
    public class DoctorService(IDbContextFactory<AppDbContext> dbContextFactory) : IDoctorService
    {
        public Task<DoctorProfile> GetDoctorByIdAsync(Guid doctorId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<DoctorProfile>> GetAllDoctorsAsync()
        {
            using var context = await dbContextFactory.CreateDbContextAsync();

            return await context.DoctorProfiles
                .Include(dp => dp.User)
                .OrderBy(dp => dp.User.Name)
                .ToListAsync();
        }

        public async Task<List<DoctorProfile>> GetDoctorByTypeAsync(DoctorType type)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();

            var response = await context.DoctorProfiles.Include(dp => dp.User)
                .Where(dp => dp.Specialty == type)
                .ToListAsync();

            return response;
        }
    }
}
