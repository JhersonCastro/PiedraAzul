using Microsoft.EntityFrameworkCore;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;
using PiedraAzul.Shared.Enums;

namespace PiedraAzul.ApplicationServices.Services
{
    public interface IDoctorService
    {
        Task<List<DoctorProfile>> GetDoctorByTypeAsync(DoctorType type);

        Task<DoctorProfile?> GetDoctorByUserIdAsync(string userId);
    }
    public class DoctorService(IDbContextFactory<AppDbContext> dbContextFactory) : IDoctorService
    {
        public async Task<DoctorProfile?> GetDoctorByUserIdAsync(string userId)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();

            return await context.DoctorProfiles
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.UserId == userId);
        }

        public async Task<List<DoctorProfile>> GetDoctorByTypeAsync(DoctorType type)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();

            return await context.DoctorProfiles
                .Include(dp => dp.User)
                .Where(dp => dp.Specialty == type)
                .ToListAsync();
        }
    }
}
