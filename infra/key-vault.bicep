@description('The name of the Key Vault')
param keyVaultName string

@description('The location for the Key Vault')
param location string = resourceGroup().location

@description('The Azure AD tenant ID for the Key Vault')
param tenantId string = subscription().tenantId

@description('Tags for the Key Vault')
param tags object = {}

@description('Object ID of the user/service principal that needs access to the Key Vault')
param principalId string = ''

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

// Grant Key Vault Secrets User role to the principal if provided
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  name: guid(keyVault.id, principalId, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

@description('The Key Vault resource ID')
output keyVaultId string = keyVault.id

@description('The Key Vault name')
output keyVaultName string = keyVault.name

@description('The Key Vault URI')
output keyVaultUri string = keyVault.properties.vaultUri
