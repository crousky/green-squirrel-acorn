# Deployment Guide - Green Squirrel Dev

This guide covers deploying the Green Squirrel Dev platform to Azure.

## Prerequisites

- Azure subscription
- Azure CLI installed
- .NET 8.0 SDK
- Google OAuth credentials
- GitHub account (for CI/CD)

## Step 1: Create Azure Resources

You can deploy infrastructure using either **Bicep templates (recommended)** or **Azure CLI commands**.

### Option A: Deploy with Bicep Templates (Recommended)

The `infra/` folder contains Bicep templates that provision all required Azure resources:

- **Cosmos DB** (serverless) with `Users` and `Projects` containers
- **Static Web App** with configured app settings
- **Application Insights** with Log Analytics workspace

#### 1.1 Deploy Infrastructure

The Bicep templates will create:
- `green-squirrel-cosmos` - Cosmos DB account
- `green-squirrel-site` - Static Web App
- `green-squirrel-insights` - Application Insights

```bash
# Deploy all resources with Bicep (uses existing resource group rg-green-squirrel)
az deployment group create \
  --resource-group rg-green-squirrel \
  --template-file infra/main.bicep \
  --parameters \
    googleClientId="YOUR_GOOGLE_CLIENT_ID" \
    googleClientSecret="YOUR_GOOGLE_CLIENT_SECRET" \
    jwtSecret="YOUR_JWT_SECRET_MIN_32_CHARACTERS"
```

#### 1.2 View Deployment Outputs

```bash
az deployment group show \
  --resource-group rg-green-squirrel \
  --name main \
  --query properties.outputs
```

---

### Option B: Deploy with Azure CLI (Manual)

If you prefer manual resource creation (uses existing resource group `rg-green-squirrel`):

#### 1.1 Create Cosmos DB Account

```bash
az cosmosdb create \
  --name green-squirrel-cosmos \
  --resource-group rg-green-squirrel \
  --default-consistency-level Session \
  --enable-automatic-failover false \
  --locations regionName=eastus2
```

#### 1.2 Create Cosmos DB Database and Containers

```bash
# Create database
az cosmosdb sql database create \
  --account-name green-squirrel-cosmos \
  --resource-group rg-green-squirrel \
  --name GreenSquirrelDev

# Create Users container
az cosmosdb sql container create \
  --account-name green-squirrel-cosmos \
  --resource-group rg-green-squirrel \
  --database-name GreenSquirrelDev \
  --name Users \
  --partition-key-path "/partitionKey"

# Create Projects container
az cosmosdb sql container create \
  --account-name green-squirrel-cosmos \
  --resource-group rg-green-squirrel \
  --database-name GreenSquirrelDev \
  --name Projects \
  --partition-key-path "/partitionKey"
```

## Step 2: Configure Google OAuth

### 2.1 Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create new project: "Green Squirrel Dev"
3. Enable Google+ API

### 2.2 Configure OAuth Consent Screen

1. Navigate to "APIs & Services" > "OAuth consent screen"
2. Select "External" user type
3. Fill in application details:
   - App name: Green Squirrel Dev
   - User support email: your-email@domain.com
   - Developer contact: your-email@domain.com
4. Add scopes: `email`, `profile`
5. Add test users (for development)

### 2.3 Create OAuth 2.0 Credentials

1. Go to "APIs & Services" > "Credentials"
2. Create "OAuth client ID"
3. Application type: "Web application"
4. Name: "Green Squirrel Dev Web"
5. Authorized JavaScript origins:
   - `https://greensquirrel.dev`
   - `http://localhost:5000` (for development)
6. Authorized redirect URIs (Azure SWA custom OIDC uses this callback pattern):
   - `https://greensquirrel.dev/.auth/login/google/callback`
   - `https://<your-swa-name>.azurestaticapps.net/.auth/login/google/callback`
   - `https://greensquirrel.dev/.auth/complete/google/callback` (alternative for custom OIDC)
   - `https://<your-swa-name>.azurestaticapps.net/.auth/complete/google/callback`
7. Save Client ID and Client Secret

## Step 3: Configure Azure Static Web App

### 3.1 Get Cosmos DB Connection String

```bash
az cosmosdb keys list \
  --name green-squirrel-cosmos \
  --resource-group rg-green-squirrel \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" \
  --output tsv
```

### 3.2 Configure Application Settings

```bash
# Get Static Web App API Key
az staticwebapp secrets list \
  --name green-squirrel-site \
  --resource-group rg-green-squirrel

# Configure settings (replace with actual values)
az staticwebapp appsettings set \
  --name green-squirrel-site \
  --resource-group rg-green-squirrel \
  --setting-names \
    CosmosDb__ConnectionString="YOUR_COSMOS_CONNECTION_STRING" \
    CosmosDb__DatabaseName="GreenSquirrelDev" \
    Google__ClientId="YOUR_GOOGLE_CLIENT_ID" \
    Google__ClientSecret="YOUR_GOOGLE_CLIENT_SECRET" \
    Jwt__Secret="YOUR_JWT_SECRET_32_CHARS_MIN" \
    Jwt__Issuer="https://greensquirrel.dev" \
    Jwt__Audience="https://greensquirrel.dev" \
    Jwt__ExpirationMinutes="1440"
```

### 3.3 Configure Custom Domain

```bash
# Add custom domain
az staticwebapp hostname set \
  --name green-squirrel-site \
  --resource-group rg-green-squirrel \
  --hostname greensquirrel.dev
```

Update DNS records:
- Type: CNAME
- Name: @
- Value: [your-static-web-app].azurestaticapps.net

## Step 4: Deploy Application

### 4.1 Manual Deployment

```bash
# Build and publish client
cd src/GreenSquirrelDev.Client
dotnet publish -c Release -o ./publish

# Build and publish functions
cd ../GreenSquirrelDev.Functions
dotnet publish -c Release -o ./publish
```

### 4.2 GitHub Actions Deployment (Recommended)

1. GitHub Actions workflow is automatically created by Azure Static Web Apps
2. Push to main branch triggers deployment
3. Workflow file location: `.github/workflows/azure-static-web-apps-*.yml`

Example workflow:

```yaml
name: Azure Static Web Apps CI/CD

on:
  push:
    branches:
      - main
  pull_request:
    types: [opened, synchronize, reopened, closed]
    branches:
      - main

jobs:
  build_and_deploy_job:
    runs-on: ubuntu-latest
    name: Build and Deploy Job
    steps:
      - uses: actions/checkout@v2
        with:
          submodules: true
      - name: Build And Deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "/src/GreenSquirrelDev.Client"
          api_location: "/src/GreenSquirrelDev.Functions"
          output_location: "wwwroot"
```

## Step 5: Verify Deployment

### 5.1 Check Static Web App

```bash
az staticwebapp show \
  --name green-squirrel-site \
  --resource-group rg-green-squirrel \
  --query "defaultHostname" \
  --output tsv
```

Visit the URL to verify the site is running.

### 5.2 Test API Endpoints

```bash
# Test auth endpoint
curl -X GET https://greensquirrel.dev/api/auth/verify \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 5.3 Test Google Sign-In

1. Navigate to https://greensquirrel.dev
2. Click "Sign in with Google"
3. Complete OAuth flow
4. Verify profile page shows user information

## Step 6: Monitor Application

### 6.1 Enable Application Insights

```bash
az staticwebapp appsettings set \
  --name green-squirrel-site \
  --resource-group rg-green-squirrel \
  --setting-names \
    APPLICATIONINSIGHTS_CONNECTION_STRING="YOUR_APP_INSIGHTS_CONNECTION_STRING"
```

### 6.2 View Logs

```bash
# View Function logs
az staticwebapp functions log \
  --name green-squirrel-site \
  --resource-group rg-green-squirrel
```

## Troubleshooting

### Issue: Google Sign-In Not Working

1. Verify OAuth credentials are correct
2. Check authorized origins include your domain
3. Ensure Google Client ID is configured in `appsettings.json`
4. Check browser console for errors

### Issue: API Returns 500 Error

1. Check Cosmos DB connection string is correct
2. Verify database and containers exist
3. Check Function App logs in Application Insights
4. Ensure JWT secret is configured

### Issue: CORS Errors

1. Verify `AllowedOrigins` includes your domain
2. Check Static Web App configuration
3. Ensure API endpoints have correct CORS headers

## Rollback

To rollback to a previous deployment:

```bash
# List deployments
az staticwebapp show \
  --name green-squirrel-site \
  --resource-group rg-green-squirrel

# Rollback via GitHub Actions by reverting commit
```

## Security Checklist

- [ ] Google OAuth credentials configured securely
- [ ] JWT secret is strong (32+ characters)
- [ ] Cosmos DB connection string stored in App Settings
- [ ] HTTPS enforced on all endpoints
- [ ] CORS configured to allow only trusted origins
- [ ] Application Insights enabled for monitoring
- [ ] Regular security updates scheduled

## Performance Optimization

- [ ] Enable CDN for static assets
- [ ] Configure caching headers
- [ ] Optimize image sizes
- [ ] Enable Blazor WASM AOT compilation
- [ ] Monitor Cosmos DB RU consumption

## Backup Strategy

- [ ] Enable Cosmos DB automatic backup
- [ ] Document configuration settings
- [ ] Store secrets in Azure Key Vault
- [ ] Regular testing of restore procedures

---

For questions or issues, contact: contact@greensquirrel.dev
