@description('The name of the Key Vault')
param keyVaultName string

@description('The location for the Key Vault')
param location string = resourceGroup().location

@description('The Azure AD tenant ID for the Key Vault')
param tenantId string = subscription().tenantId

@description('Tags for the Key Vault')
param tags object = {}

@description('Enable public network access')
param publicNetworkAccess string = 'Enabled'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    enableRbacAuthorization: true
    publicNetworkAccess: publicNetworkAccess
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

@description('The Key Vault resource ID')
output keyVaultId string = keyVault.id

@description('The Key Vault name')
output keyVaultName string = keyVault.name

@description('The Key Vault URI')
output keyVaultUri string = keyVault.properties.vaultUri
