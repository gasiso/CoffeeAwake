// =============================================================================
// CoffeeAwake — NativeMethods.cs
// Win32 API interop for SetThreadExecutionState.
//
// This is the ONLY system call CoffeeAwake makes. It does NOT:
//   • Simulate keyboard or mouse input
//   • Modify the registry
//   • Change power plans
//   • Require administrator privileges
//
// Reference: https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate
// =============================================================================

using System.Runtime.InteropServices;

namespace CoffeeAwake.Native;

/// <summary>
/// Flags passed to <see cref="NativeMethods.SetThreadExecutionState"/>.
/// </summary>
[Flags]
internal enum ExecutionState : uint
{
    /// <summary>
    /// Resets the execution state to normal (lets the system sleep naturally).
    /// Always combine with <see cref="Continuous"/> to clear a previous request.
    /// </summary>
    None = 0x00000000,

    /// <summary>
    /// Prevents the system (CPU/RAM) from entering sleep.
    /// </summary>
    SystemRequired = 0x00000001,

    /// <summary>
    /// Prevents the display from turning off.
    /// </summary>
    DisplayRequired = 0x00000002,

    /// <summary>
    /// Makes the request persist until explicitly cleared.
    /// Without this flag the request only applies to the current burst of activity.
    /// </summary>
    Continuous = 0x80000000,
}

/// <summary>
/// Thin P/Invoke wrapper. Sealed + static so no instance is ever created.
/// </summary>
internal static class NativeMethods
{
    // kernel32 is always available; no DllImport attribute quirks needed.
    [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    /// <summary>
    /// Requests that Windows keep the system (and optionally the display) awake.
    /// </summary>
    /// <param name="state">One or more <see cref="ExecutionState"/> flags ORed together.</param>
    /// <returns>The previous execution state, or 0 on failure.</returns>
    internal static ExecutionState SetState(ExecutionState state)
        => (ExecutionState)SetThreadExecutionState((uint)state);

    /// <summary>
    /// Convenience: prevent both system sleep and display sleep (persistent).
    /// </summary>
    internal static bool KeepAwake()
    {
        var result = SetState(
            ExecutionState.Continuous |
            ExecutionState.SystemRequired |
            ExecutionState.DisplayRequired);
        return result != ExecutionState.None;
    }

    /// <summary>
    /// Convenience: release the persistent awake request and let Windows decide.
    /// Safe to call even if <see cref="KeepAwake"/> was never called.
    /// </summary>
    internal static bool ReleaseAwake()
    {
        var result = SetState(ExecutionState.Continuous | ExecutionState.None);
        return result != ExecutionState.None;
    }
}
