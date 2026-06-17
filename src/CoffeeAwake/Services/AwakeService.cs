// =============================================================================
// CoffeeAwake — AwakeService.cs
// Manages the awake/sleep state via SetThreadExecutionState.
//
// Design principles:
//   • No timers used for the "keep-awake" mechanism — the OS call is sufficient.
//   • A System.Threading.Timer is used ONLY for timed sessions (1h/2h/4h).
//   • IDisposable releases all resources cleanly.
//   • Thread-safe state transitions via lock.
// =============================================================================

using CoffeeAwake.Native;

namespace CoffeeAwake.Services;

/// <summary>
/// Raised whenever the awake state changes so UI components can react.
/// </summary>
public sealed class AwakeStateChangedEventArgs(bool isAwake) : EventArgs
{
    public bool IsAwake { get; } = isAwake;
}

/// <summary>
/// Encapsulates all logic for controlling the system's awake state.
/// Exposes a simple Activate / Deactivate / ToggleAsync API.
/// </summary>
public sealed class AwakeService : IDisposable
{
    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------
    private readonly object _lock = new();
    private bool _isAwake;
    private System.Threading.Timer? _sessionTimer;
    private bool _disposed;

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------
    /// <summary>Raised on the calling thread when the awake state changes.</summary>
    public event EventHandler<AwakeStateChangedEventArgs>? StateChanged;

    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------
    /// <summary>Whether the system is currently being kept awake.</summary>
    public bool IsAwake
    {
        get { lock (_lock) return _isAwake; }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Activates the keep-awake state indefinitely.
    /// </summary>
    public void Activate()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            CancelSessionTimer();

            if (NativeMethods.KeepAwake())
            {
                SetState(true);
            }
        }
    }

    /// <summary>
    /// Activates the keep-awake state for a fixed duration, then auto-deactivates.
    /// </summary>
    /// <param name="duration">How long to stay awake.</param>
    public void ActivateFor(TimeSpan duration)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            CancelSessionTimer();

            if (NativeMethods.KeepAwake())
            {
                SetState(true);

                // Timer fires once after the duration elapses.
                _sessionTimer = new System.Threading.Timer(
                    callback: _ => Deactivate(),
                    state: null,
                    dueTime: duration,
                    period: Timeout.InfiniteTimeSpan);
            }
        }
    }

    /// <summary>
    /// Deactivates the keep-awake state and lets the OS manage power normally.
    /// </summary>
    public void Deactivate()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            CancelSessionTimer();
            NativeMethods.ReleaseAwake();
            SetState(false);
        }
    }

    /// <summary>Toggles between active and inactive states.</summary>
    public void Toggle()
    {
        if (IsAwake) Deactivate();
        else Activate();
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private void SetState(bool newState)
    {
        // Only raise the event when the state actually changes.
        if (_isAwake == newState) return;
        _isAwake = newState;

        // Marshal event to UI thread if needed (caller is responsible, but we
        // raise here so the service stays decoupled from WinForms internals).
        StateChanged?.Invoke(this, new AwakeStateChangedEventArgs(newState));
    }

    private void CancelSessionTimer()
    {
        _sessionTimer?.Dispose();
        _sessionTimer = null;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(AwakeService));
    }

    // -------------------------------------------------------------------------
    // IDisposable
    // -------------------------------------------------------------------------

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;

            // Always release the awake state so Windows behaves normally
            // even if the app crashes or is killed via Task Manager.
            // (Windows also cleans this up when the process exits, but being
            //  explicit is good practice and documents intent clearly.)
            CancelSessionTimer();
            NativeMethods.ReleaseAwake();
        }
    }
}
