# Green Squirrel Dev - Portfolio & Projects Platform

A modern portfolio site showcasing innovative developer tools and applications, built with Blazor WebAssembly and Azure Functions.

## 🌳 Overview

Green Squirrel Dev is a portfolio and project showcase platform featuring:

- **Pace Calculator** - A tool for runners to calculate paces, times, and distances
- **HiveReader** - Send web articles to Kindle with Chrome extension support (In Development)
- User authentication via Google OAuth
- Chrome extension integration support
- Responsive, modern design with green squirrel branding

## 🛠️ Tech Stack

### Frontend
- **Blazor WebAssembly** - C# on the client-side
- **HTML5 & CSS3** - Modern, responsive design
- **JavaScript** - Interop for Google Sign-In

### Backend
- **Azure Functions** - Serverless C# APIs
- **.NET 8.0** - Latest LTS version
- **Azure Cosmos DB** - NoSQL database
- **Google OAuth 2.0** - Authentication provider

### Hosting
- **Azure Static Web Apps** - Frontend hosting with CDN
- **Azure Functions** - Backend API hosting
- **Custom Domain** - greensquirrel.dev

## 📁 Project Structure

```
green-squirrel-acorn/
├── src/
│   ├── GreenSquirrelDev.Client/          # Blazor WASM frontend
│   │   ├── Pages/                        # Razor pages
│   │   ├── Components/                   # Reusable components
│   │   ├── Services/                     # Client services
│   │   └── wwwroot/                      # Static files
│   ├── GreenSquirrelDev.Functions/       # Azure Functions backend
│   │   ├── Auth/                         # Authentication functions
│   │   ├── Users/                        # User management functions
│   │   └── Services/                     # Backend services
│   └── GreenSquirrelDev.Shared/          # Shared models & DTOs
│       ├── Models/                       # Domain models
│       └── DTOs/                         # Data transfer objects
├── greensquirrel-dev-prd.md             # Product Requirements Document
└── README.md                            # This file
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Azure subscription
- Google OAuth credentials
- Cosmos DB instance (or emulator for local development)

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/green-squirrel-acorn.git
   cd green-squirrel-acorn
   ```

2. **Configure Azure Functions**
   - Navigate to `src/GreenSquirrelDev.Functions/`
   - Update `local.settings.json` with your credentials:
     - Cosmos DB connection string
     - Google OAuth Client ID and Secret
     - JWT secret key (minimum 32 characters)

3. **Configure Blazor Client**
   - Update `src/GreenSquirrelDev.Client/wwwroot/appsettings.json`
   - Add your Google OAuth Client ID

4. **Run the Functions API**
   ```bash
   cd src/GreenSquirrelDev.Functions
   func start
   ```

5. **Run the Blazor Client**
   ```bash
   cd src/GreenSquirrelDev.Client
   dotnet run
   ```

6. **Open browser**
   - Navigate to `https://localhost:5000` (or the port shown)

### Building for Production

```bash
# Build the solution
dotnet build --configuration Release

# Publish Blazor WASM
cd src/GreenSquirrelDev.Client
dotnet publish -c Release

# Publish Azure Functions
cd ../GreenSquirrelDev.Functions
dotnet publish -c Release
```

## 🔐 Authentication Flow

### Web Authentication
1. User clicks "Sign in with Google"
2. Google OAuth consent screen appears
3. After approval, ID token is sent to Azure Functions
4. Functions validate token with Google
5. User created/updated in Cosmos DB
6. JWT token returned to client
7. Client stores token for API calls

### Chrome Extension Authentication
1. Extension initiates auth by calling `/api/auth/extension/initiate`
2. Opens new tab with auth URL
3. User completes Google OAuth
4. Token passed to extension via messaging
5. Extension stores token securely

## 📡 API Endpoints

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/auth/google` | POST | No | Exchange Google OAuth token for JWT |
| `/api/auth/verify` | GET | Yes | Verify JWT token validity |
| `/api/auth/extension/initiate` | POST | No | Start extension auth flow |
| `/api/auth/extension/complete` | POST | No | Complete extension auth |
| `/api/users/me` | GET | Yes | Get current user profile |
| `/api/users/me` | PUT | Yes | Update user profile |

## 🎨 Design System

### Color Palette
- **Primary**: Forest Green (#228B22)
- **Secondary**: Warm Brown (#8B4513)
- **Accent**: Bright Orange/Amber (#FFA500)
- **Background**: Off-white (#F8F9FA)
- **Text**: Dark Gray (#333333)

### Typography
- **Headings**: Inter, sans-serif
- **Body**: Inter, sans-serif
- **Code**: Courier New, monospace

## 🚢 Deployment

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed deployment instructions.

## 🧪 Testing

```bash
# Run unit tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📝 Contributing

This is a personal project, but suggestions and feedback are welcome! Please open an issue for any bugs or feature requests.

## 📄 License

Copyright © 2025 Green Squirrel Dev. All rights reserved.

## 🐿️ About

Green Squirrel Dev is an individual software development company focused on creating innovative, user-friendly tools. Like a squirrel gathering acorns, we carefully curate projects that solve real problems for real people.

## 🔗 Links

- **Main Site**: https://greensquirrel.dev
- **Pace Calculator**: https://pacecalculator.greensquirrel.dev
- **HiveReader**: https://hive-reader.greensquirrel.dev (Coming Soon)

## 📧 Contact

- **Email**: contact@greensquirrel.dev
- **GitHub**: https://github.com/greensquirreldev

---

Built with 💚 by Green Squirrel Dev