using './main.bicep'

// Location configuration
param location = 'eastus2'

// JWT configuration
param jwtIssuer = 'https://greensquirrel.dev'
param jwtAudience = 'https://greensquirrel.dev'

// Key Vault access (set via pipeline)
// param keyVaultAccessPrincipalId = '' // Service principal object ID for Key Vault access
