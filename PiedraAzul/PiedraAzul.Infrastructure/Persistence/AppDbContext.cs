using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Entities;
using PiedraAzul.Domain.Entities.Audit;
using PiedraAzul.Domain.Entities.Config;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Infrastructure.Auth;
using PiedraAzul.Infrastructure.DataProtection;
using PiedraAzul.Infrastructure.Identity;


namespace PiedraAzul.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
        public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<AppointmentRescheduleRecord> AppointmentRescheduleRecords => Set<AppointmentRescheduleRecord>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<DoctorAvailabilitySlot> DoctorAvailabilitySlots => Set<DoctorAvailabilitySlot>();
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<GuestVerificationSession> GuestVerificationSessions => Set<GuestVerificationSession>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<PasskeyCredential> PasskeyCredentials => Set<PasskeyCredential>();
        public DbSet<UserMFAConfiguration> UserMFAConfigurations => Set<UserMFAConfiguration>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // Una sola cédula por usuario registrado (no aplica a soft-deleted ni a cédula vacía).
            // El lado de invitados ya está garantizado por la PK de Patients (Id = cédula del guest).
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.IdentificationNumber)
                .IsUnique()
                .HasFilter("\"IdentificationNumber\" <> '' AND \"IsDeleted\" = false");
        }
    }
}