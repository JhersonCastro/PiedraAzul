using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;
using PiedraAzul.Shared.Enums;

namespace PiedraAzul.Seeders
{
    public static class DbSeeder
    {
        private const string DoctorRole = "Doctor";
        private const string PatientRole = "Patient";
        private const string AdminRole = "Admin";

        private sealed record DoctorSeed(string Email, string Name, DoctorType Specialty);
        private sealed record UserSeed(string Email, string Name, string Role);

        public static async Task SeedAsync(
            IDbContextFactory<AppDbContext> dbContextFactory,
            UserManager<ApplicationUser> userManager)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();

            if (await context.DoctorProfiles.AnyAsync())
                return;

            // ✅ FECHA BASE EN UTC (00:00)
            var todayUtc = DateTime.UtcNow.Date;

            // ================= USERS =================
            var doctorSeeds = new[]
            {
                new DoctorSeed("doctor1@test.com","Dra. Laura",DoctorType.NaturalMedicine),
                new DoctorSeed("doctor2@test.com","Dr. Andrés",DoctorType.Chiropractic),
                new DoctorSeed("doctor3@test.com","Dra. Valentina",DoctorType.Optometry),
                new DoctorSeed("doctor4@test.com","Dr. Sebastián",DoctorType.Physiotherapy)
            };

            var patientSeeds = new[]
            {
                new UserSeed("patient1@test.com","Paciente Juan",PatientRole),
                new UserSeed("patient2@test.com","Paciente Camila",PatientRole),
                new UserSeed("patient3@test.com","Paciente Mateo",PatientRole),
                new UserSeed("patient4@test.com","Paciente Sofía",PatientRole)
            };

            var adminSeeds = new[]
            {
                new UserSeed("admin1@test.com","Admin 1",AdminRole),
                new UserSeed("admin2@test.com","Admin 2",AdminRole)
            };

            var doctorUsers = new List<(ApplicationUser User, DoctorType Specialty)>();
            var patientUsers = new List<ApplicationUser>();

            foreach (var d in doctorSeeds)
            {
                var user = await CreateUser(userManager, d.Email, d.Name, DoctorRole);
                doctorUsers.Add((user, d.Specialty));
            }

            foreach (var p in patientSeeds)
                patientUsers.Add(await CreateUser(userManager, p.Email, p.Name, PatientRole));

            foreach (var a in adminSeeds)
                await CreateUser(userManager, a.Email, a.Name, AdminRole);

            // ================= PROFILES =================
            foreach (var doctor in doctorUsers)
            {
                if (!await context.DoctorProfiles.AnyAsync(x => x.UserId == doctor.User.Id))
                {
                    context.DoctorProfiles.Add(new DoctorProfile
                    {
                        UserId = doctor.User.Id,
                        Specialty = doctor.Specialty
                    });
                }
            }

            foreach (var p in patientUsers)
            {
                if (!await context.PatientProfiles.AnyAsync(x => x.UserId == p.Id))
                {
                    context.PatientProfiles.Add(new PatientProfile
                    {
                        UserId = p.Id
                    });
                }
            }

            await context.SaveChangesAsync();

            // ================= SLOTS =================
            var allowedDays = new[]
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday
            };

            foreach (var doctor in doctorUsers)
            {
                foreach (var day in allowedDays)
                {
                    if (await context.DoctorAvailabilitySlots
                        .AnyAsync(x => x.DoctorUserId == doctor.User.Id && x.DayOfWeek == day))
                        continue;

                    foreach (var range in GetRanges(day))
                    {
                        var current = range.Start;

                        while (current < range.End)
                        {
                            context.DoctorAvailabilitySlots.Add(new DoctorAvailabilitySlot
                            {
                                Id = Guid.NewGuid(),
                                DoctorUserId = doctor.User.Id,
                                DayOfWeek = day,
                                StartTime = current, // ✅ LOCAL (NO UTC)
                                EndTime = current.Add(TimeSpan.FromMinutes(30))
                            });

                            current = current.Add(TimeSpan.FromMinutes(30));
                        }
                    }
                }
            }

            await context.SaveChangesAsync();

            // ================= APPOINTMENTS =================
            if (!await context.Appointments.AnyAsync())
            {
                var dates = Enumerable.Range(0, 3)
                    .Select(i => todayUtc.AddDays(i))
                    .Where(d => d.DayOfWeek != DayOfWeek.Sunday)
                    .ToList();

                var appointments = new List<Appointment>();
                var random = new Random();

                foreach (var doctor in doctorUsers)
                {
                    foreach (var date in dates)
                    {
                        var slots = await context.DoctorAvailabilitySlots
                            .Where(x => x.DoctorUserId == doctor.User.Id &&
                                        x.DayOfWeek == date.DayOfWeek)
                            .OrderBy(x => x.StartTime)
                            .ToListAsync();

                        if (slots.Count == 0)
                            continue;

                        var selectedSlots = slots
                            .OrderBy(_ => random.Next())
                            .Take(date == dates[0] ? 5 : 2)
                            .ToList();

                        foreach (var slot in selectedSlots)
                        {
                            var patient = patientUsers[random.Next(patientUsers.Count)];

                            appointments.Add(new Appointment
                            {
                                Id = Guid.NewGuid(),
                                DoctorUserId = doctor.User.Id,
                                PatientUserId = patient.Id,
                                DoctorAvailabilitySlotId = slot.Id,
                                Date = date, // ✅ SOLO FECHA (00:00 UTC)
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                context.Appointments.AddRange(appointments);
                await context.SaveChangesAsync();
            }
        }

        private static async Task<ApplicationUser> CreateUser(
            UserManager<ApplicationUser> um,
            string email,
            string name,
            string role)
        {
            var u = await um.FindByEmailAsync(email);
            if (u != null) return u;

            u = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = name
            };

            var r = await um.CreateAsync(u, "Password123!");
            if (!r.Succeeded)
                throw new Exception(string.Join(",", r.Errors.Select(e => e.Description)));

            await um.AddToRoleAsync(u, role);
            return u;
        }

        private static IEnumerable<(TimeSpan Start, TimeSpan End)> GetRanges(DayOfWeek d)
        {
            if (d >= DayOfWeek.Monday && d <= DayOfWeek.Friday)
            {
                yield return (new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0));
                yield return (new TimeSpan(14, 0, 0), new TimeSpan(18, 0, 0));
            }
            else if (d == DayOfWeek.Saturday)
            {
                yield return (new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0));
            }
        }
    }
}