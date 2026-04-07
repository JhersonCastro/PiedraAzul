using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Entities.Config;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Infrastructure.Identity;
using PiedraAzul.Infrastructure.Persistence.Auth;

namespace PiedraAzul.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<DoctorAvailabilitySlot> Slots => Set<DoctorAvailabilitySlot>();
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}