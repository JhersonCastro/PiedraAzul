using Microsoft.AspNetCore.SignalR.Client;
using PiedraAzul.Contracts.DTOs;

namespace PiedraAzul.Client.Services.RealTimeServices
{
    public interface IAppointmentHubService : IAsyncDisposable
    {
        event Func<string, DateTime, string, Task>? SlotLocked;
        event Func<string, DateTime, Task>? SlotReleased;
        event Func<List<SlotStateDto>, Task>? HydrateSlots;
        event Func<string, DateTime, Task>? IsExpired;

        Task StartAsync();
        Task JoinGroup(string doctorId, DateTime date);
        Task<bool> SetLocked(string doctorId, string slotId, DateTime date);
        Task<bool> SetReleased(string doctorId, string slotId, DateTime date);
        Task<List<LockedSlotInfo>> GetLockedSlots(string doctorId, DateTime date);
        Task InitializeSlots(string doctorId, DateTime date, List<SlotStateDto> slots); // Nuevo método
        string GetConnectionId();
    }
    public class LockedSlotInfo
    {
        public string SlotId { get; set; } = "";
        public string LockedBy { get; set; } = "";
        public DateTime LockExpiresAt { get; set; }
    }

    public class AppointmentHubService : IAppointmentHubService
    {
        private readonly HubConnection _hubConnection;
        private bool _disposed;
        private readonly string _hubUrl;

        public event Func<string, DateTime, string, Task>? SlotLocked;
        public event Func<string, DateTime, Task>? SlotReleased;
        public event Func<List<SlotStateDto>, Task>? HydrateSlots;
        public event Func<string, DateTime, Task>? IsExpired;

        public AppointmentHubService(string signalRBaseUrl)
        {
            _hubUrl = $"{signalRBaseUrl.TrimEnd('/')}/hubs/appointments";
            Console.WriteLine($"[SignalR] Inicializando hub en URL: {_hubUrl}");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.FromSeconds(0),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30)
                })
                .Build();

            RegisterHandlers();
            RegisterConnectionEvents();
        }

        private void RegisterHandlers()
        {
            _hubConnection.On<string, DateTime, string>("SlotLocked", async (slotId, date, lockedBy) =>
            {
                Console.WriteLine($"[SignalR] Recibido evento SlotLocked - SlotId: {slotId}, Date: {date}, LockedBy: {lockedBy}");
                if (SlotLocked != null)
                    await SlotLocked.Invoke(slotId, date, lockedBy);
            });

            _hubConnection.On<string, DateTime>("SlotReleased", async (slotId, date) =>
            {
                Console.WriteLine($"[SignalR] Recibido evento SlotReleased - SlotId: {slotId}, Date: {date}");
                if (SlotReleased != null)
                    await SlotReleased.Invoke(slotId, date);
            });

            _hubConnection.On<List<SlotStateDto>>("HydrateSlots", async (slots) =>
            {
                Console.WriteLine($"[SignalR] Recibido evento HydrateSlots - Slots: {slots?.Count ?? 0}");
                if (HydrateSlots != null)
                    await HydrateSlots.Invoke(slots);
            });

            _hubConnection.On<string, DateTime>("IsExpired", async (slotId, date) =>
            {
                Console.WriteLine($"[SignalR] Recibido evento IsExpired - SlotId: {slotId}, Date: {date}");
                if (IsExpired != null)
                    await IsExpired.Invoke(slotId, date);
            });
        }

        private void RegisterConnectionEvents()
        {
            _hubConnection.Reconnected += async (connectionId) =>
            {
                Console.WriteLine($"[SignalR] Reconectado. ConnectionId: {connectionId}");
                await Task.CompletedTask;
            };

            _hubConnection.Reconnecting += (error) =>
            {
                Console.WriteLine($"[SignalR] Reconectando... Error: {error?.Message}");
                return Task.CompletedTask;
            };

            _hubConnection.Closed += (error) =>
            {
                Console.WriteLine($"[SignalR] Conexión cerrada. Error: {error?.Message}");
                return Task.CompletedTask;
            };
        }

        public async Task StartAsync()
        {
            try
            {
                Console.WriteLine($"[SignalR] StartAsync llamado. Estado actual: {_hubConnection.State}");
                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                    Console.WriteLine($"[SignalR] Conectado exitosamente. ConnectionId: {_hubConnection.ConnectionId}");
                }
                else
                {
                    Console.WriteLine($"[SignalR] Ya estaba conectado. Estado: {_hubConnection.State}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error en StartAsync: {ex.Message}");
                throw;
            }
        }

        public async Task JoinGroup(string doctorId, DateTime date)
        {
            try
            {
                Console.WriteLine($"[SignalR] JoinGroup llamado - DoctorId: {doctorId}, Date: {date}, Estado: {_hubConnection.State}");
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("JoinGroup", doctorId, date);
                    Console.WriteLine($"[SignalR] JoinGroup exitoso");
                }
                else
                {
                    Console.WriteLine($"[SignalR] No se puede unir al grupo - Estado: {_hubConnection.State}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error en JoinGroup: {ex.Message}");
            }
        }

        public async Task<bool> SetLocked(string doctorId, string slotId, DateTime date)
        {
            try
            {
                Console.WriteLine($"[SignalR] SetLocked llamado - DoctorId: {doctorId}, SlotId: {slotId}, Date: {date}, Estado: {_hubConnection.State}");

                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    var result = await _hubConnection.InvokeAsync<bool>("SetLocked", doctorId, slotId, date);
                    Console.WriteLine($"[SignalR] SetLocked resultado: {result}");
                    return result;
                }
                else
                {
                    Console.WriteLine($"[SignalR] No se puede llamar SetLocked - Estado de conexión: {_hubConnection.State}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error en SetLocked: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SetReleased(string doctorId, string slotId, DateTime date)
        {
            try
            {
                Console.WriteLine($"[SignalR] SetReleased llamado - DoctorId: {doctorId}, SlotId: {slotId}, Date: {date}");
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    return await _hubConnection.InvokeAsync<bool>("SetReleased", doctorId, slotId, date);
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error en SetReleased: {ex.Message}");
                return false;
            }
        }

        public async Task<List<LockedSlotInfo>> GetLockedSlots(string doctorId, DateTime date)
        {
            try
            {
                Console.WriteLine($"[SignalR] GetLockedSlots llamado - DoctorId: {doctorId}, Date: {date}");
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    var result = await _hubConnection.InvokeAsync<List<LockedSlotInfo>>("GetLockedSlots", doctorId, date);
                    Console.WriteLine($"[SignalR] GetLockedSlots resultado: {result?.Count ?? 0} slots");
                    return result ?? new List<LockedSlotInfo>();
                }
                return new List<LockedSlotInfo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error en GetLockedSlots: {ex.Message}");
                return new List<LockedSlotInfo>();
            }
        }

        public string GetConnectionId()
        {
            return _hubConnection.ConnectionId ?? string.Empty;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_hubConnection != null)
                {
                    Console.WriteLine("[SignalR] DisposeAsync llamado");
                    await _hubConnection.DisposeAsync();
                }
            }
        }

        public async Task InitializeSlots(string doctorId, DateTime date, List<SlotStateDto> slots)
        {
            try
            {
                Console.WriteLine($"[SignalR] InitializeSlots llamado - DoctorId: {doctorId}, Date: {date}, Slots: {slots.Count}");
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("InitializeSlots", doctorId, date, slots);
                    Console.WriteLine($"[SignalR] InitializeSlots exitoso");
                }
                else
                {
                    Console.WriteLine($"[SignalR] No se puede inicializar slots - Estado: {_hubConnection.State}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error en InitializeSlots: {ex.Message}");
            }
        }
    }
}