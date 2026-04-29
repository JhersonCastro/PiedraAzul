using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;
using PiedraAzul.Infrastructure.Identity;
using PiedraAzul.Infrastructure.Persistence;

namespace PiedraAzul.Seeders
{
    public static class DbSeeder
    {
        private const string DoctorRole = "Doctor";
        private const string PatientRole = "Patient";
        private const string AdminRole = "Admin";

        public static async Task SeedAsync(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // 🔥 1. ROLES
            await EnsureRoles(roleManager);

            // 🔥 2. EVITAR RESEED
            if (await context.Doctors.AnyAsync())
                return;

            // 🔥 3. USERS BASE
            var patientUser = await CreateUser(userManager, "patient@test.com", "Paciente Demo", PatientRole);
            var adminUser = await CreateUser(userManager, "admin@test.com", "Admin Demo", AdminRole);

            // 🔥 4. DOCTORES
            var doctors = new List<Doctor>();

            foreach (DoctorType type in Enum.GetValues(typeof(DoctorType)))
            {
                var email = $"doctor_{type.ToString().ToLower()}@test.com";
                var name = $"Dr. {type}";

                var user = await CreateUser(userManager, email, name, DoctorRole);

                var doctor = new Doctor(
                    user.Id,
                    type,
                    $"LIC-{Guid.NewGuid().ToString()[..6]}",
                    $"Especialista en {type}");

                // 🔥 DISPONIBILIDAD COMPLETA

                foreach (var day in new[]
                {
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday
                })
                {
                    // mañana
                    AddSlots(doctor, day,
                        new TimeSpan(8, 0, 0),
                        new TimeSpan(12, 0, 0));

                    // tarde
                    AddSlots(doctor, day,
                        new TimeSpan(14, 0, 0),
                        new TimeSpan(18, 0, 0));
                }

                // sábado
                AddSlots(doctor, DayOfWeek.Saturday,
                    new TimeSpan(8, 0, 0),
                    new TimeSpan(12, 0, 0));

                context.Doctors.Add(doctor);
                doctors.Add(doctor);
            }

            await context.SaveChangesAsync();

            // 🔥 5. CITAS DEMO
            var random = new Random();

            foreach (var doctor in doctors)
            {
                var slots = doctor.Slots.ToList();
                if (!slots.Any()) continue;

                int appointmentsCount = random.Next(3, 7);

                for (int i = 0; i < appointmentsCount; i++)
                {
                    var slot = slots[random.Next(slots.Count)];

                    var today = DateTime.Now.Date;
                    var targetDay = slot.DayOfWeek;

                    int daysToAdd = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;
                    if (daysToAdd == 0) daysToAdd = 7;

                    var validDate = today.AddDays(daysToAdd);

                    var appointment = Appointment.Create(
                        slot,
                        DateOnly.FromDateTime(validDate),
                        doctor.Id,
                        patientUser.Id,
                        null);

                    context.Appointments.Add(appointment);
                }
            }

            await context.SaveChangesAsync();
        }

        // ================= HELPERS =================

        private static void AddSlots(
            Doctor doctor,
            DayOfWeek day,
            TimeSpan start,
            TimeSpan end,
            int minutesStep = 30)
        {
            var current = start;

            while (current < end)
            {
                var next = current.Add(TimeSpan.FromMinutes(minutesStep));

                if (next > end)
                    break;

                doctor.AddAvailability(day, current, next);

                current = next;
            }
        }

        private static async Task EnsureRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { AdminRole, DoctorRole, PatientRole };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
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