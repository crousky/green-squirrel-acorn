using './main.bicep'

// Environment configuration
param environment = 'dev'
param baseName = 'greensquirrel'
param location = 'centralus'

// Google OAuth credentials (replace with actual values or use Key Vault references)
param googleClientId = '' // Set via Azure CLI or pipeline variable
param googleClientSecret = '' // Set via Azure CLI or pipeline variable

// JWT configuration (replace with actual value or use Key Vault reference)
param jwtSecret = '' // Set via Azure CLI or pipeline variable (min 32 characters)
param jwtIssuer = 'https://greensquirrel.dev'
param jwtAudience = 'https://greensquirrel.dev'
