// =============================================================================
// CoffeeAwake — StartupService.cs
// Manages the "Start with Windows" option via the user's Startup registry key.
//
// Uses HKEY_CURRENT_USER (no admin rights needed).
// The key is: HKCU\Software\Microsoft\Windows\CurrentVersion\Run
// =============================================================================

using Microsoft.Win32;

namespace CoffeeAwake.Services;

/// <summary>
/// Reads and writes the Windows Startup registry entry for CoffeeAwake.
/// Operates entirely within HKEY_CURRENT_USER — no elevation required.
/// </summary>
public static class StartupService
{
    private const string RegistryKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Run";

    private const string AppName = "CoffeeAwake";

    /// <summary>Returns true if CoffeeAwake is registered to start with Windows.</summary>
    public static bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
                return key?.GetValue(AppName) is string;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Registers or unregisters CoffeeAwake from the Windows Startup key.
    /// </summary>
    /// <param name="enable">True to enable; false to disable.</param>
    /// <returns>True if the operation succeeded.</returns>
    public static bool SetEnabled(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (key is null) return false;

            if (enable)
            {
                // Use the actual executable path so it works from any location.
                var exePath = Environment.ProcessPath
                              ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

                if (string.IsNullOrEmpty(exePath)) return false;

                // Wrap in quotes to handle paths with spaces.
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
