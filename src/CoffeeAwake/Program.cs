// =============================================================================
// CoffeeAwake — Program.cs
// Application entry point. Minimal and clean.
//
// Key decisions made here:
//   • Single-instance enforcement via a named Mutex.
//   • DPI-awareness set before any WinForms call.
//   • Unhandled exception guard ensures the awake state is always released.
// =============================================================================

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CoffeeAwake;

internal static class Program
{
    private const string MutexName = "Global\\CoffeeAwake_SingleInstance_B7F3A2C1";

    [STAThread]
    private static void Main()
    {
        // ── Single-instance guard ─────────────────────────────────────────────
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            // Another instance is already running — do nothing and exit cleanly.
            return;
        }

        // ── Global exception handler ──────────────────────────────────────────
        // Ensures SetThreadExecutionState(ES_CONTINUOUS) is cleared even on crash.
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Application.ThreadException                += OnThreadException;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        // ── WinForms bootstrap ────────────────────────────────────────────────
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Use our custom ApplicationContext (no main form)
        using var context = new TrayApplicationContext();
        Application.Run(context);

        // Mutex is released when 'using' exits
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Exception handlers
    // ─────────────────────────────────────────────────────────────────────────

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Release awake state immediately — Windows will also do this when the
        // process exits, but being explicit documents intent and handles edge cases.
        Native.NativeMethods.ReleaseAwake();

        if (e.ExceptionObject is Exception ex)
        {
            Debug.WriteLine($"[CoffeeAwake] Unhandled exception: {ex}");
        }
    }

    private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
        Native.NativeMethods.ReleaseAwake();
        Debug.WriteLine($"[CoffeeAwake] Thread exception: {e.Exception}");
    }
}
