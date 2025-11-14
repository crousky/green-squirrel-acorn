# VS Code Configuration

This directory contains VS Code configuration files for debugging and building the Green Squirrel Dev project.

## Debug Configurations

### Individual Components

1. **Launch Blazor Client** - Launches only the Blazor WebAssembly frontend
   - Opens in Edge browser at https://localhost:5001
   - Useful for frontend-only development

2. **Launch Azure Functions** - Launches only the Azure Functions backend
   - Runs on http://localhost:7071
   - Useful for API-only development

### Full Application

3. **Launch Full Application** - Compound configuration that launches both:
   - Azure Functions backend (port 7071)
   - Blazor WASM frontend (port 5001)
   - **Recommended for full-stack development**

## How to Use

### Starting the Application

1. Open the project in VS Code
2. Press `F5` or go to Run and Debug (Ctrl+Shift+D)
3. Select "Launch Full Application" from the dropdown
4. Click the green play button

### Building

- **Build All**: Press `Ctrl+Shift+B` (default build task)
- **Clean**: Run task `clean` from Command Palette (Ctrl+Shift+P)
- **Restore**: Run task `restore` to restore NuGet packages

### Watching for Changes

- Run task `watch-client` to enable hot reload for the Blazor client
- The Functions host also supports hot reload when running in debug mode

## Prerequisites

Make sure you have installed:

1. .NET 8.0 SDK
2. Azure Functions Core Tools (for Functions debugging)
3. Recommended VS Code extensions (see extensions.json)

## Troubleshooting

### Functions won't start
- Ensure `local.settings.json` exists in `src/GreenSquirrelDev.Functions/`
- Check that Azure Functions Core Tools are installed: `func --version`

### Blazor client build errors
- Run `dotnet restore` from the solution root
- Check that all NuGet packages are restored

### Can't attach debugger
- Ensure the process is running
- Try restarting VS Code
- Check that .NET SDK is properly installed

## Configuration Files

- **launch.json** - Debug configurations
- **tasks.json** - Build and run tasks
- **settings.json** - Workspace settings
- **extensions.json** - Recommended extensions
