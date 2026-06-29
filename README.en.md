# ☕ CoffeeAwake

<div align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4.svg" alt=".NET 8.0">
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6.svg" alt="Windows">
  <img src="https://img.shields.io/badge/license-MIT-green.svg" alt="License">
</div>

<p align="center">
  <b>🇺🇸 English</b> | <a href="README.md">🇧🇷 Português</a>
</p>

<br>

> **Lightweight Windows tray utility** that keeps your PC awake using the official Win32 `SetThreadExecutionState` API — no input simulation, no registry tweaks, no power-plan changes, no admin rights needed.

---

## Features

| Feature | Details |
|---|---|
| **Stay awake** | Prevents system sleep AND display timeout |
| **Timed sessions** | Auto-deactivate after 1h / 2h / 4h |
| **Tray-only** | No main window — lives quietly in the notification area |
| **Double-click toggle** | Quick on/off directly from the tray icon |
| **Start with Windows** | Optional autostart (HKCU — no UAC prompt) |
| **Single-instance** | Second launch does nothing |
| **Self-contained EXE** | One file, no .NET runtime required on target machine |

---

## How It Works

CoffeeAwake calls a single Win32 API:

```csharp
// Activate
SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);

// Deactivate
SetThreadExecutionState(ES_CONTINUOUS);
```

Windows automatically resets this when the process exits, so the system will never be permanently stuck in an awake state. CoffeeAwake also explicitly resets the state on normal exit and on unhandled exceptions.

---

## Project Structure

```
CoffeeAwake/
├── LICENSE
├── README.md
└── src/
    └── CoffeeAwake/
        ├── CoffeeAwake.csproj       # Project file
        ├── app.manifest             # No UAC, DPI-aware
        ├── Program.cs               # Entry point, single-instance mutex
        ├── TrayApplicationContext.cs # Tray icon, context menu, state wiring
        ├── Native/
        │   └── NativeMethods.cs     # P/Invoke for SetThreadExecutionState
        ├── Services/
        │   ├── AwakeService.cs      # Core logic, timed sessions, IDisposable
        │   └── StartupService.cs    # HKCU autostart registry helper
        └── UI/
            └── IconFactory.cs       # GDI+ icon generation (no external assets)
```

---

## Requirements

- **Windows 10 / 11** (x64 or ARM64)
- **.NET 8 SDK** — [download](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Build & Run (Development)

```powershell
# Clone / navigate to the project
cd src/CoffeeAwake

# Run directly
dotnet run
```

---

## Publish as Self-Contained EXE

### Windows x64 (recommended for most PCs)

```powershell
dotnet publish src/CoffeeAwake/CoffeeAwake.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishReadyToRun=true `
    -o ./publish/win-x64
```

### Windows ARM64 (Surface Pro X, Copilot+ PCs)

```powershell
dotnet publish src/CoffeeAwake/CoffeeAwake.csproj `
    -c Release `
    -r win-arm64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -o ./publish/win-arm64
```

The output `CoffeeAwake.exe` in the `publish/` folder is fully standalone — copy it anywhere and run. No installer, no dependencies.

---

## Usage

1. **Launch** `CoffeeAwake.exe` — a coffee cup icon appears in the system tray.
2. **Double-click** the icon to toggle awake mode on/off.
3. **Right-click** for the full menu:
   - Ativar / Desativar
   - Manter acordado por 1h / 2h / 4h
   - Iniciar com o Windows
   - Sair

---

## Security & Safety

- ✅ No admin privileges required  
- ✅ No keyboard/mouse simulation  
- ✅ No power plan modification  
- ✅ No permanent registry side-effects (startup entry is optional and user-controlled)  
- ✅ Awake state is always cleared on exit, even on crash  
- ✅ Single-instance enforced via named Mutex  
- ✅ Zero third-party dependencies  

---

## 📄 License

This project is licensed under the MIT License. Feel free to use, modify, and distribute.
