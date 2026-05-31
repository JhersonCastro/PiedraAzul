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

    public bool IsEasy => CurrentMode == UIMode.Easy;

    public void SetMode(UIMode mode)
    {
        if (CurrentMode == mode) return;
        CurrentMode = mode;
        OnModeChanged?.Invoke();
    }

    public void Toggle() => SetMode(CurrentMode == UIMode.Modern ? UIMode.Easy : UIMode.Modern);
}
