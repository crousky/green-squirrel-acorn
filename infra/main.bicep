param environment string = 'prod'

@description('The location for all resources')
param location string = resourceGroup().location

@description('JWT Issuer URL')
param jwtIssuer string = 'https://greensquirrel.dev'

@description('JWT Audience URL')
param jwtAudience string = 'https://greensquirrel.dev'

// Variables - using existing resource naming convention
var cosmosDbAccountName = 'green-squirrel-db-${environment}'
var staticWebAppName = 'green-squirrel-site'
var appInsightsName = 'green-squirrel-insights'
var logAnalyticsName = 'green-squirrel-logs'
var keyVaultName = 'kv-grnsqrl-${environment}'

var tags = {
  project: 'green-squirrel-dev'
  managedBy: 'bicep'
}

// Key Vault Module
module keyVault 'key-vault.bicep' = {
  name: 'keyVault-${uniqueString(resourceGroup().id)}'
  params: {
    keyVaultName: keyVaultName
    location: location
    tags: tags
  }
}

// Application Insights Module
module appInsights 'application-insights.bicep' = {
  name: 'appInsights-${uniqueString(resourceGroup().id)}'
  params: {
    appInsightsName: appInsightsName
    logAnalyticsWorkspaceName: logAnalyticsName
    location: location
    tags: tags
  }
}

// Cosmos DB Module
module cosmosDb 'cosmos-db.bicep' = {
  name: 'cosmosDb-${uniqueString(resourceGroup().id)}'
  params: {
    cosmosDbAccountName: cosmosDbAccountName
    location: location
    tags: tags
  }
}

// Get Cosmos DB connection string
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosDbAccountName
  dependsOn: [
    cosmosDb
  ]
}

// Static Web App Module
module staticWebApp 'static-web-app.bicep' = {
  name: 'staticWebApp-${uniqueString(resourceGroup().id)}'
  params: {
    staticWebAppName: staticWebAppName
    location: location
    sku: 'Free'
    tags: tags
    cosmosDbConnectionString: cosmosDbAccount.listConnectionStrings().connectionStrings[0].connectionString
    cosmosDbDatabaseName: 'GreenSquirrelDev'
    keyVaultName: keyVaultName
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    appInsightsConnectionString: appInsights.outputs.connectionString
  }
  dependsOn: [
    cosmosDb
    keyVault
  ]
}

// Outputs
@description('The Static Web App default hostname')
output staticWebAppHostname string = staticWebApp.outputs.defaultHostname

@description('The Cosmos DB account endpoint')
output cosmosDbEndpoint string = cosmosDb.outputs.endpoint

@description('The Application Insights connection string')
output appInsightsConnectionString string = appInsights.outputs.connectionString

@description('The Key Vault name')
output keyVaultName string = keyVault.outputs.keyVaultName

@description('The Static Web App managed identity principal ID')
output staticWebAppPrincipalId string = staticWebApp.outputs.principalId

@description('The resource group name')
output resourceGroupName string = resourceGroup().name

@description('Deployment instructions')
output deploymentInstructions string = '''
Deployment complete! Next steps:
1. Store secrets in Key Vault (${keyVault.outputs.keyVaultName}):
   - google-client-id
   - google-client-secret
   - jwt-secret (min 32 characters)
2. Configure your GitHub repository to deploy to: ${staticWebApp.outputs.defaultHostname}
3. Update Google OAuth redirect URIs to include: https://${staticWebApp.outputs.defaultHostname}/.auth/login/google/callback
4. Verify the application by visiting: https://${staticWebApp.outputs.defaultHostname}
'''
