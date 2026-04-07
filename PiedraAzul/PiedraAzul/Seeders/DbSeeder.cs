using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Entities.Shared.Enums;
using PiedraAzul.Infrastructure.Identity;
using PiedraAzul.Infrastructure.Persistence;

namespace PiedraAzul.Seeders
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            if (await context.Doctors.AnyAsync())
                return;

            var doctorUser = await CreateUser(userManager, "doctor@test.com", "Dr. Demo", "Doctor");
            var patientUser = await CreateUser(userManager, "patient@test.com", "Paciente Demo", "Patient");
            var adminUser = await CreateUser(userManager, "admin@test.com", "Admin Demo", "Admin");

            var doctor = new Doctor(
                doctorUser.Id,
                DoctorType.Chiropractic,
                "LIC-123",
                "Es un doctor, no se que mas poner");

            doctor.AddAvailability(DayOfWeek.Monday, new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0));
            doctor.AddAvailability(DayOfWeek.Tuesday, new TimeSpan(14, 0, 0), new TimeSpan(18, 0, 0));

            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var firstSlot = doctor.Slots.First();

            var appointment = Appointment.Create(
                firstSlot,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                doctor.Id,
                patientUser.Id,
                null);

            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();
        }

        private static async Task<ApplicationUser> CreateUser(
            UserManager<ApplicationUser> userManager,
            string email,
            string name,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null) return user;

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = name
            };

            var result = await userManager.CreateAsync(user, "Password123!");
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(user, role);
            return user;
        }
    }
}