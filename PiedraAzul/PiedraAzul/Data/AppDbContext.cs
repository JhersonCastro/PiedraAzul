using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Data.Models;

namespace PiedraAzul.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<DoctorAvailabilitySlot> DoctorAvailabilitySlots { get; set; }
        public DbSet<DoctorProfile> DoctorProfiles { get; set; }
        public DbSet<PatientProfile> PatientProfiles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }
        public DbSet<PatientGuest> PatientGuests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // APPOINTMENTS
            // =========================

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.DoctorAvailabilitySlot)
                .WithMany()
                .HasForeignKey(a => a.DoctorAvailabilitySlotId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => new { a.DoctorAvailabilitySlotId, a.Date })
                .IsUnique();


            // =========================
            // DOCTOR AVAILABILITY
            // =========================

            modelBuilder.Entity<DoctorAvailabilitySlot>()
                .HasOne(s => s.Doctor)
                .WithMany()
                .HasForeignKey(s => s.DoctorUserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================
            // PROFILES (1:1 PK = FK)
            // =========================

            modelBuilder.Entity<DoctorProfile>()
                .HasKey(d => d.UserId);

            modelBuilder.Entity<DoctorProfile>()
                .HasOne(d => d.User)
                .WithOne(u => u.DoctorProfile)
                .HasForeignKey<DoctorProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<PatientProfile>()
                .HasKey(p => p.UserId);

            modelBuilder.Entity<PatientProfile>()
                .HasOne(p => p.User)
                .WithOne(u => u.PatientProfile)
                .HasForeignKey<PatientProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}