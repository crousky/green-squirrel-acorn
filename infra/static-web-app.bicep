@description('The name of the Static Web App')
param staticWebAppName string

@description('The location for the Static Web App')
param location string = resourceGroup().location

@description('The SKU for the Static Web App')
@allowed([
  'Free'
  'Standard'
])
param sku string = 'Standard'

@description('Tags to apply to resources')
param tags object = {}

@description('The Cosmos DB connection string')
@secure()
param cosmosDbConnectionString string = ''

@description('The Cosmos DB database name')
param cosmosDbDatabaseName string = 'GreenSquirrelDev'

@description('Key Vault name for secret references')
param keyVaultName string

@description('JWT Issuer')
param jwtIssuer string = 'https://greensquirrel.dev'

@description('JWT Audience')
param jwtAudience string = 'https://greensquirrel.dev'

@description('JWT Token expiration in minutes')
param jwtExpirationMinutes int = 1440

@description('Application Insights connection string')
@secure()
param appInsightsConnectionString string = ''

@description('Google OAuth Client ID')
@secure()
param googleClientId string

@description('Google OAuth Client Secret')
@secure()
param googleClientSecret string

// Static Web App
resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  tags: tags
  sku: {
    name: sku
    tier: sku
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    buildProperties: {
      appLocation: '/src/GreenSquirrelDev.Client'
      apiLocation: '/src/GreenSquirrelDev.Functions'
      outputLocation: 'wwwroot'
    }
  }
}

// App Settings for the Static Web App
resource staticWebAppSettings 'Microsoft.Web/staticSites/config@2023-12-01' = {
  parent: staticWebApp
  name: 'appsettings'
  properties: {
    CosmosDb__ConnectionString: cosmosDbConnectionString
    CosmosDb__DatabaseName: cosmosDbDatabaseName
    CosmosDb__UsersContainer: 'Users'
    CosmosDb__ProjectsContainer: 'Projects'
    // Azure Static Web Apps built-in authentication
    GOOGLE_CLIENT_ID: googleClientId
    GOOGLE_CLIENT_SECRET_APP_SETTING_NAME: googleClientSecret
    // Azure Functions custom authentication
    Google__ClientId: googleClientId
    Google__ClientSecret: googleClientSecret
    Jwt__Secret: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=jwt-secret)'
    Jwt__Issuer: jwtIssuer
    Jwt__Audience: jwtAudience
    Jwt__ExpirationMinutes: string(jwtExpirationMinutes)
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsConnectionString
  }
}

// Outputs
@description('The Static Web App default hostname')
output defaultHostname string = staticWebApp.properties.defaultHostname

@description('The Static Web App resource ID')
output resourceId string = staticWebApp.id

@description('The Static Web App name')
output name string = staticWebApp.name

@description('The Static Web App managed identity principal ID')
output principalId string = staticWebApp.identity.principalId
