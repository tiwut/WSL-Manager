# WSL Manager

WSL Manager is a premium, native Windows WPF desktop application designed to manage your Windows Subsystem for Linux (WSL) distributions with a beautiful, modern GUI. It provides a visual dashboard and administration utilities similar to Distrobox/Distroshelf on Linux.

---

## Features

* **Distro Dashboard**:
  * Visual cards showing all registered WSL distros.
  * Real-time status indicators (Running/Stopped) and version badges (WSL 1/WSL 2).
  * Direct launch terminal, terminate instance, set default distro, duplicate (clone), export (.tar backup), and delete (unregister) distros.
  * Quick-launch File Explorer directly inside any distro's root directory (`\\wsl.localhost\`).
* **Interactive Run Command**:
  * Execute shell commands inside any target distro and view the output streamed in real-time in a monospace command box.
* **Distro Installer Catalog**:
  * Official WSL online catalog listing distributions like Ubuntu, Debian, Kali, SLES, and Oracle Linux.
  * Direct one-click installation with external terminal redirection (supports interactive UNIX user credentials creation).
  * Custom Rootfs Import tool to register custom `.tar` packages into any local destination directory.
* **Global WSL Config Editor**:
  * Edit your `%USERPROFILE%\.wslconfig` engine configuration with a simple visual editor (adjust memory limits, CPU cores, swap space, localhost forwarding, nested virtualization, and DNS tunneling).
* **System Doctor**:
  * Automated helpers to enable Windows WSL optional features, Virtual Machine Platform components, set default WSL versions, and run kernel package updates. Actions triggering optional feature enablement automatically request Windows UAC administrator elevation.
* **Premium Visuals**:
  * Deep dark-mode slate theme with customized scrollbars, buttons, textboxes, and combo boxes.
  * Customized title bar integrated directly into the borderless visual layout.
  * Clean, vector-based SVG geometry paths for all interface icons.

---

## Getting Started

### Prerequisites
* Windows 10 (Build 19041 and higher) or Windows 11.
* .NET 8.0 Desktop Runtime (not required if running the Self-Contained published `.exe`).

### Running the App
1. Go to your workspace root directory.
2. Run `WSLMgr.exe`.
3. If no distros are registered, follow the prompt to navigate to the **System Doctor** or **Install / Import** catalog to bootstrap your environment!

---

## Build & Compile from Source

You can use the interactive script at the root directory to clean or build the project, or run the standard `dotnet` CLI commands manually.

### Using the Build Script
Run the interactive build utility from the root of the workspace:
```cmd
build.cmd
```
It provides options to:
1. **Clean Build**: Cleans compile output, removes `bin` and `obj` folders, and deletes build artifacts from the root directory.
2. **Build (Debug)**: Compiles a debug version of the application.
3. **Publish (Release Single-File)**: Performs a release publish and copies the self-contained `WSLMgr.exe` and native runtime DLLs to the root directory for execution.

### Manual Development Build
```bash
cd WSLMgr
dotnet build
```

### Manual Release Publish
This compiles the application into a standalone, single-file `.exe` including all required framework assemblies:
```bash
dotnet publish WSLMgr/WSLMgr.csproj -c Release
```
The output executable and native assemblies will be generated under:
`WSLMgr/bin/Release/net8.0-windows/win-x64/publish/`

---

## Project Structure

```
WSL-MGR/
│
├── WSLMgr.exe                  # Compiled self-contained executable
├── wpfgfx_cor3.dll             # Native WPF graphics support assembly
├── PresentationNative_cor3.dll # WPF native core loader
├── D3DCompiler_47_cor3.dll     # Direct3D shader runtime
│
└── WSLMgr/                     # Source Directory
    ├── WSLMgr.csproj           # C# WPF Project file
    ├── App.xaml / App.xaml.cs  # App entrypoint and merged resource dictionaries
    ├── Theme.xaml              # Color palette, window styles, and custom control templates
    ├── Converters.cs           # Binding converters (InverseBoolean, Visibility)
    ├── MainWindow.xaml / .cs   # Sidebar layout, dashboard views, catalog list, and settings tab
    ├── WslDistro.cs            # Data models for distro instances
    ├── WslBridge.cs            # WSL process caller and registry lookup bridge
    ├── WslConfigManager.cs     # INI parser and writer for .wslconfig files
    │
    ├── CloneDialog.xaml / .cs  # Dialog box for distro duplication
    └── RunCommandDialog.xaml   # Monospace shell runner dialog
```

---

## License

Distributed under the MIT License. See `LICENSE` for more details.
