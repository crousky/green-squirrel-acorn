# Build Guide - Green Squirrel Dev

This guide will help you build and run the Green Squirrel Dev project on your local machine.

## Prerequisites

### Required Software

1. **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
   ```bash
   # Verify installation
   dotnet --version
   # Should show: 8.0.x
   ```

2. **Azure Functions Core Tools v4** - [Installation guide](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
   ```bash
   # Verify installation
   func --version
   # Should show: 4.x
   ```

3. **Visual Studio Code** (Recommended) - [Download here](https://code.visualstudio.com/)

### Optional but Recommended

- **Azure Cosmos DB Emulator** - For local database testing
- **Git** - For version control

## Quick Start (VS Code)

### 1. Install Recommended Extensions

When you open the project in VS Code, you'll be prompted to install recommended extensions. Click "Install All" or install them manually:

- C# Dev Kit
- Azure Functions
- Azure Tools

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Configure Settings

**For Functions (Backend):**

Edit `src/GreenSquirrelDev.Functions/local.settings.json`:

```json
{
  "Values": {
    "CosmosDb__ConnectionString": "YOUR_COSMOS_CONNECTION_STRING",
    "Google__ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "Google__ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET",
    "Jwt__Secret": "YOUR_32_CHAR_SECRET_KEY_HERE_MINIMUM"
  }
}
```

**For Client (Frontend):**

Edit `src/GreenSquirrelDev.Client/wwwroot/appsettings.json`:

```json
{
  "GoogleClientId": "YOUR_GOOGLE_CLIENT_ID"
}
```

### 4. Run the Application

**Option A: Using VS Code Debugger (Recommended)**

1. Press `F5` or go to Run and Debug (Ctrl+Shift+D)
2. Select "Launch Full Application"
3. Click the green play button

This will start both:
- Backend API at http://localhost:7071
- Frontend at https://localhost:5001

**Option B: Using Terminal**

Open two terminal windows:

Terminal 1 (Functions):
```bash
cd src/GreenSquirrelDev.Functions
func start
```

Terminal 2 (Blazor Client):
```bash
cd src/GreenSquirrelDev.Client
dotnet run
```

### 5. Access the Application

Open your browser to: https://localhost:5001

## Building from Command Line

### Build Entire Solution

```bash
# From project root
dotnet build
```

### Build Individual Projects

```bash
# Client only
dotnet build src/GreenSquirrelDev.Client/GreenSquirrelDev.Client.csproj

# Functions only
dotnet build src/GreenSquirrelDev.Functions/GreenSquirrelDev.Functions.csproj

# Shared library only
dotnet build src/GreenSquirrelDev.Shared/GreenSquirrelDev.Shared.csproj
```

### Clean Build

```bash
dotnet clean
dotnet build
```

### Publish for Production

```bash
# Client
cd src/GreenSquirrelDev.Client
dotnet publish -c Release -o ./publish

# Functions
cd ../GreenSquirrelDev.Functions
dotnet publish -c Release -o ./publish
```

## Common Build Issues and Solutions

### Issue: "Package restore failed"

**Solution:**
```bash
dotnet restore
# Or force restore
dotnet restore --force
```

### Issue: "The SDK 'Microsoft.NET.Sdk.BlazorWebAssembly' specified could not be found"

**Solution:** Install .NET 8.0 SDK or update to latest version
```bash
dotnet --list-sdks
```

### Issue: "Azure Functions Core Tools not found"

**Solution:** Install Azure Functions Core Tools
```bash
# Windows (using npm)
npm install -g azure-functions-core-tools@4 --unsafe-perm true

# macOS (using Homebrew)
brew tap azure/functions
brew install azure-functions-core-tools@4

# Linux
# See: https://docs.microsoft.com/azure/azure-functions/functions-run-local
```

### Issue: "Google Sign-In not working"

**Solution:**
1. Ensure Google OAuth credentials are configured in both `local.settings.json` and `appsettings.json`
2. Add http://localhost:5000 to authorized origins in Google Cloud Console
3. Add http://localhost:5000/auth/callback to authorized redirect URIs

### Issue: "CORS errors when calling API"

**Solution:** Ensure Functions are running on port 7071 and Client is configured to call http://localhost:7071/api

### Issue: "Cannot find type or namespace 'GreenSquirrelDev.Shared'"

**Solution:** Build the Shared project first
```bash
dotnet build src/GreenSquirrelDev.Shared/GreenSquirrelDev.Shared.csproj
dotnet build
```

### Issue: Service Worker errors

**Solution:** The service worker is optional. If you encounter errors, you can comment out or remove the ServiceWorker ItemGroup in the .csproj file temporarily.

## Project Structure

```
GreenSquirrelDev.sln              # Solution file
├── src/
│   ├── GreenSquirrelDev.Client/   # Blazor WASM frontend
│   │   ├── Pages/                 # Razor pages
│   │   ├── Components/            # Reusable components
│   │   ├── Services/              # Client-side services
│   │   └── wwwroot/               # Static files
│   ├── GreenSquirrelDev.Functions/ # Azure Functions backend
│   │   ├── Auth/                  # Auth endpoints
│   │   ├── Users/                 # User endpoints
│   │   └── Services/              # Backend services
│   └── GreenSquirrelDev.Shared/   # Shared models & DTOs
```

## Development Workflow

### 1. Make Changes

Edit files in your preferred editor (VS Code recommended)

### 2. Hot Reload

When using `dotnet watch` or running via VS Code debugger, changes are automatically reloaded:

- Blazor Client: UI changes reload automatically
- Functions: Code changes trigger rebuild

### 3. Debug

Set breakpoints in VS Code and press F5 to debug both frontend and backend simultaneously

### 4. Test

```bash
# Run tests (when available)
dotnet test
```

### 5. Commit

```bash
git add .
git commit -m "Your commit message"
git push
```

## Building for Different Environments

### Development

```bash
dotnet build -c Debug
```

### Production

```bash
dotnet build -c Release
```

## NuGet Package Versions

All packages use version 8.0.0 to match .NET 8.0. If you need to update packages:

```bash
dotnet list package --outdated
dotnet add package <PackageName> --version <Version>
```

## Additional Resources

- [Blazor WebAssembly Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
- [Azure Functions C# Developer Guide](https://docs.microsoft.com/azure/azure-functions/functions-dotnet-class-library)
- [.NET CLI Documentation](https://docs.microsoft.com/dotnet/core/tools/)

## Getting Help

If you encounter issues not covered here:

1. Check the error message carefully
2. Ensure all prerequisites are installed and updated
3. Try cleaning and rebuilding: `dotnet clean && dotnet build`
4. Check the project README.md for additional information

## Next Steps

After successfully building:

1. Review DEPLOYMENT.md for production deployment
2. Configure your Azure resources (see DEPLOYMENT.md)
3. Set up Google OAuth credentials
4. Replace placeholder images with actual assets
5. Customize the content to match your needs

---

Happy coding! 🐿️💚
