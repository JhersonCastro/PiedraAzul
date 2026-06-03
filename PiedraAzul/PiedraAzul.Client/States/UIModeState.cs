namespace PiedraAzul.Client.States;

public enum UIMode { Modern, Easy }

/// <summary>
/// Scoped state that holds the current UI mode (Modern or Easy).
/// Components subscribe to OnModeChanged to react to switches.
/// </summary>
public class UIModeState
{
    public UIMode CurrentMode { get; private set; } = UIMode.Modern;

    public event Action? OnModeChanged;

    /// <summary>
    /// Fired once when the user completes the first-time UI setup modal.
    /// Each page subscribes to this to start its own tour if the chosen mode is Modern.
    /// </summary>
    public event Action<UIMode>? OnSetupComplete;

    public bool IsEasy => CurrentMode == UIMode.Easy;

    public void SetMode(UIMode mode)
    {
        if (CurrentMode == mode) return;
        CurrentMode = mode;
        OnModeChanged?.Invoke();
    }

    public void NotifySetupComplete(UIMode chosenMode) =>
        OnSetupComplete?.Invoke(chosenMode);

    public void Toggle() => SetMode(CurrentMode == UIMode.Modern ? UIMode.Easy : UIMode.Modern);
}
