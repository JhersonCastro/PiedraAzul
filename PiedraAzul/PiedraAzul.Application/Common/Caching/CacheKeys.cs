using System;

namespace PiedraAzul.Application.Common.Caching;

/// <summary>
/// Construcción centralizada de keys y tags del cache, para que lectura e invalidación
/// usen exactamente las mismas cadenas.
///
/// Niveles de granularidad:
///  - key directa  (doc:{id}:day:{fecha}) → se invalida puntual con Remove() al crear/cancelar una cita
///  - tag doc:{id}                        → "martillo del doctor" (cambió su horario): borra sus días+slots
///  - tag days:{id}                       → la lista de días disponibles de un doctor
///  - tag docs:{esp}                      → la lista de doctores de una especialidad (disponibilidad/altas/bajas)
/// </summary>
public static class CacheKeys
{
    // ── Keys ───────────────────────────────────────────────────────────
    public static string DoctorDaySlots(string doctorId, DateOnly date)
        => $"doc:{doctorId}:day:{date:yyyyMMdd}";

    public static string DoctorAvailableDays(string doctorId, DateOnly from, int numberOfDays)
        => $"doc:{doctorId}:days:{from:yyyyMMdd}:{numberOfDays}";

    public static string DoctorRecurringSlots(string doctorId)
        => $"doc:{doctorId}:slots";

    public static string DoctorsBySpecialty(int specialty, bool onlyAvailable)
        => $"docs:{specialty}:{onlyAvailable}";

    // ── Tags ───────────────────────────────────────────────────────────
    public static string TagDoctor(string doctorId) => $"tag:doc:{doctorId}";
    public static string TagDoctorDays(string doctorId) => $"tag:days:{doctorId}";
    public static string TagSpecialty(int specialty) => $"tag:docs:{specialty}";
}
