using Microsoft.EntityFrameworkCore;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;
using PiedraAzul.Shared.Enums;

namespace PiedraAzul.ApplicationServices.Services
{
    public interface IDoctorService
    {
        Task<List<DoctorProfile>> GetDoctorByTypeAsync(DoctorType type);
    }
    public class DoctorService(IDbContextFactory<AppDbContext> dbContextFactory) : IDoctorService
    {
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
