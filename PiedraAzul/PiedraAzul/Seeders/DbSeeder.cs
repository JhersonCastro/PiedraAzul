using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;
using PiedraAzul.Shared.Enums;

namespace PiedraAzul.Seeders
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            IDbContextFactory<AppDbContext> dbContextFactory,
            UserManager<ApplicationUser> userManager)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();

            // Evitar duplicar
            if (await context.DoctorProfiles.AnyAsync())
                return;

            // =========================
            // 1. USERS (IDENTITY)
            // =========================
            var doctorEmail = "doctor@test.com";
            var patientEmail = "patient@test.com";

            var doctorUser = await userManager.FindByEmailAsync(doctorEmail);
            if (doctorUser == null)
            {
                doctorUser = new ApplicationUser
                {
                    UserName = doctorEmail,
                    Email = doctorEmail,
                    Name = "Dr. Demo"
                };

                var result = await userManager.CreateAsync(doctorUser, "Password123!");
                if (!result.Succeeded)
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

                await userManager.AddToRoleAsync(doctorUser, "Doctor");
            }

            var patientUser = await userManager.FindByEmailAsync(patientEmail);
            if (patientUser == null)
            {
                patientUser = new ApplicationUser
                {
                    UserName = patientEmail,
                    Email = patientEmail,
                    Name = "Paciente Demo"
                };

                var result = await userManager.CreateAsync(patientUser, "Password123!");
                if (!result.Succeeded)
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // =========================
            // 2. PROFILES (PK = FK)
            // =========================
            var doctorProfile = new DoctorProfile
            {
                UserId = doctorUser.Id,
                Specialty = DoctorType.NaturalMedicine
            };

            var patientProfile = new PatientProfile
            {
                UserId = patientUser.Id
            };

            context.DoctorProfiles.Add(doctorProfile);
            context.PatientProfiles.Add(patientProfile);

            // =========================
            // 3. AVAILABILITY SLOTS
            // =========================
            var todayUtc = DateTime.UtcNow.Date;
            var dayOfWeek = todayUtc.DayOfWeek;

            var availabilitySlots = new List<DoctorAvailabilitySlot>();

            var start = new TimeSpan(8, 0, 0);
            var end = new TimeSpan(12, 0, 0);
            var current = start;

            while (current < end)
            {
                availabilitySlots.Add(new DoctorAvailabilitySlot
                {
                    Id = Guid.NewGuid(),
                    DoctorUserId = doctorUser.Id,
                    DayOfWeek = dayOfWeek,
                    StartTime = current,
                    EndTime = current.Add(TimeSpan.FromMinutes(30))
                });

                current = current.Add(TimeSpan.FromMinutes(30));
            }

            context.DoctorAvailabilitySlots.AddRange(availabilitySlots);

            // =========================
            // 4. APPOINTMENTS
            // =========================
            var appointments = availabilitySlots
                .Take(2)
                .Select(slot => new Appointment
                {
                    Id = Guid.NewGuid(),

                    DoctorUserId = doctorUser.Id,
                    PatientUserId = patientUser.Id,

                    DoctorAvailabilitySlotId = slot.Id,
                    Date = todayUtc,

                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            context.Appointments.AddRange(appointments);

            // =========================
            // SAVE
            // =========================
            await context.SaveChangesAsync();
        }
    }
}