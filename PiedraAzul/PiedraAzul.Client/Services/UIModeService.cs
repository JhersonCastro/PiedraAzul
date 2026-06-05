using Microsoft.JSInterop;
using PiedraAzul.Client.States;

namespace PiedraAzul.Client.Services;

/// <summary>
/// Manages UI mode persistence.
/// Reads from localStorage on startup, writes on every change.
/// Future: also syncs with the user's BD profile.
/// </summary>
public class UIModeService
{
    private const string StorageKey = "uiMode";

    private readonly IJSRuntime _js;
    private readonly UIModeState _state;

    public UIModeService(IJSRuntime js, UIModeState state)
    {
        _js = js;
        _state = state;
    }

    /// <summary>
    /// Load the stored mode from localStorage.
    /// Must be called after JS interop is available (OnAfterRenderAsync / firstRender).
    /// </summary>
    public async Task LoadModeAsync()
    {
        try
        {
            var stored = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (stored is not null && Enum.TryParse<UIMode>(stored, true, out var mode))
                _state.SetMode(mode);
        }
        catch
        {
            // localStorage may not be available during prerender — silently ignore
        }
    }

    /// <summary>
    /// Set a new mode and persist it immediately to localStorage.
    /// </summary>
    public async Task SetModeAsync(UIMode mode)
    {
        _state.SetMode(mode);
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, mode.ToString());
            await _js.InvokeVoidAsync("setUiModeCookie", mode.ToString());
        }
        catch { }
    }

    /// <summary>Toggle between Modern and Easy and persist.</summary>
    public Task ToggleAsync() =>
        SetModeAsync(_state.CurrentMode == UIMode.Modern ? UIMode.Easy : UIMode.Modern);
}
