using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.Schedule;
using Microsoft.JSInterop;
using PiedraAzul.Client.Services.GraphQLServices;
using System.Text.Json;

namespace PiedraAzul.Client.Services.Schedule;

public interface IScheduleConfigService
{
    Task<ScheduleConfigModel?> GetByDoctorIdAsync(string doctorId);
    Task<Result<bool>> SaveAsync(ScheduleConfigModel config);
}

public class ScheduleConfigService(GraphQLHttpClient client, IJSRuntime jsRuntime) : IScheduleConfigService
{
    private readonly Dictionary<string, ScheduleConfigModel> _fallbackStore = new();
    private const string LocalStoragePrefix = "schedule-config:";

    public async Task<ScheduleConfigModel?> GetByDoctorIdAsync(string doctorId)
    {
        if (string.IsNullOrWhiteSpace(doctorId))
        {
            return null;
        }

        var fromBackend = await TryGetFromBackendAsync(doctorId);
        if (fromBackend is not null)
        {
            _fallbackStore[doctorId] = Clone(fromBackend);
            return fromBackend;
        }

        var fromLocal = await TryGetFromLocalStorageAsync(doctorId);
        if (fromLocal is not null)
        {
            _fallbackStore[doctorId] = Clone(fromLocal);
            return fromLocal;
        }

        if (_fallbackStore.TryGetValue(doctorId, out var existing))
        {
            return Clone(existing);
        }

        var defaultConfig = BuildDefaultConfig(doctorId);
        _fallbackStore[doctorId] = Clone(defaultConfig);
        return defaultConfig;
    }

    public async Task<Result<bool>> SaveAsync(ScheduleConfigModel config)
    {
        if (config is null || string.IsNullOrWhiteSpace(config.DoctorId))
        {
            return Result<bool>.Failure(new ErrorResult("DoctorId es requerido.", "Validation"));
        }

        var backendSave = await TrySaveToBackendAsync(config);
        if (backendSave.IsSuccess)
        {
            _fallbackStore[config.DoctorId] = Clone(config);
            return backendSave;
        }

        var localPersisted = await TrySaveToLocalStorageAsync(config);
        if (localPersisted)
        {
            _fallbackStore[config.DoctorId] = Clone(config);
            return Result<bool>.Failure(new ErrorResult("Backend no disponible. Configuración guardada localmente en este navegador.", "ScheduleConfigLocalFallback"));
        }

        return backendSave;
    }

    private async Task<ScheduleConfigModel?> TryGetFromBackendAsync(string doctorId)
    {
        const string query = """
            query GetScheduleConfigByDoctorId($doctorId: String!) {
                scheduleConfigByDoctorId(doctorId: $doctorId) {
                    doctorId
                    bookingWindowWeeks
                    intervalMinutes
                    availability {
                        dayOfWeek
                        isEnabled
                        startTime
                        endTime
                    }
                }
            }
            """;

        try
        {
            return await client.ExecuteAsync<ScheduleConfigModel>(
                query,
                new { doctorId },
                "scheduleConfigByDoctorId");
        }
        catch
        {
            return null;
        }
    }

    private async Task<Result<bool>> TrySaveToBackendAsync(ScheduleConfigModel config)
    {
        const string mutation = """
            mutation SaveScheduleConfig($input: ScheduleConfigInput!) {
                saveScheduleConfig(input: $input)
            }
            """;

        try
        {
            var success = await client.ExecuteAsync<bool>(
                mutation,
                new
                {
                    input = new
                    {
                        doctorId = config.DoctorId,
                        bookingWindowWeeks = config.BookingWindowWeeks,
                        intervalMinutes = config.IntervalMinutes,
                        availability = config.Availability.Select(day => new
                        {
                            dayOfWeek = day.DayOfWeek,
                            isEnabled = day.IsEnabled,
                            startTime = day.StartTime,
                            endTime = day.EndTime
                        }).ToList()
                    }
                },
                "saveScheduleConfig");

            return Result<bool>.Success(success);
        }
        catch
        {
            return Result<bool>.Failure(new ErrorResult("API de horario no disponible aún.", "ScheduleConfigApi"));
        }
    }



    private async Task<ScheduleConfigModel?> TryGetFromLocalStorageAsync(string doctorId)
    {
        try
        {
            var key = LocalStoragePrefix + doctorId;
            var raw = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return JsonSerializer.Deserialize<ScheduleConfigModel>(raw);
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> TrySaveToLocalStorageAsync(ScheduleConfigModel config)
    {
        try
        {
            var key = LocalStoragePrefix + config.DoctorId;
            var payload = JsonSerializer.Serialize(config);
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, payload);
            return true;
        }
        catch
        {
            return false;
        }
    }
    private static ScheduleConfigModel BuildDefaultConfig(string doctorId)
    {
        return new ScheduleConfigModel
        {
            DoctorId = doctorId,
            BookingWindowWeeks = 4,
            IntervalMinutes = 15,
            Availability =
            [
                new() { DayOfWeek = DayOfWeek.Monday, IsEnabled = true, StartTime = new(8,0,0), EndTime = new(17,0,0) },
                new() { DayOfWeek = DayOfWeek.Tuesday, IsEnabled = true, StartTime = new(8,0,0), EndTime = new(17,0,0) },
                new() { DayOfWeek = DayOfWeek.Wednesday, IsEnabled = true, StartTime = new(8,0,0), EndTime = new(17,0,0) },
                new() { DayOfWeek = DayOfWeek.Thursday, IsEnabled = true, StartTime = new(8,0,0), EndTime = new(17,0,0) },
                new() { DayOfWeek = DayOfWeek.Friday, IsEnabled = true, StartTime = new(8,0,0), EndTime = new(17,0,0) }
            ]
        };
    }

    private static ScheduleConfigModel Clone(ScheduleConfigModel model) => new()
    {
        DoctorId = model.DoctorId,
        BookingWindowWeeks = model.BookingWindowWeeks,
        IntervalMinutes = model.IntervalMinutes,
        Availability = model.Availability
            .Select(x => new AvailabilityDayModel
            {
                DayOfWeek = x.DayOfWeek,
                IsEnabled = x.IsEnabled,
                StartTime = x.StartTime,
                EndTime = x.EndTime
            })
            .ToList()
    };
}
