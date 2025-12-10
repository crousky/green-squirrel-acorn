# Key Vault Setup for Green Squirrel Dev

## Overview

The infrastructure now uses Azure Key Vault to securely store sensitive configuration values including:
- Google OAuth Client ID
- Google OAuth Client Secret  
- JWT Secret for token signing

## Architecture

1. **Key Vault Creation**: The Key Vault is created as part of the Bicep infrastructure deployment
2. **Secret Storage**: Secrets are stored in GitHub Actions secrets and uploaded to Key Vault during deployment
3. **Secret Retrieval**: The Bicep deployment retrieves secrets from Key Vault and passes them to the Static Web App

## Required GitHub Secrets

Add the following secrets to your GitHub repository:

1. **AZURE_CLIENT_ID**: Azure service principal client ID
2. **AZURE_TENANT_ID**: Azure tenant ID
3. **AZURE_SUBSCRIPTION_ID**: Azure subscription ID

Note: Google OAuth and JWT secrets are now stored directly in Azure Key Vault and not in GitHub.

## Deployment Flow

The GitHub Actions workflow performs the following steps:

1. **Azure Login**: Authenticates using federated credentials
2. **Deploy Infrastructure**: Deploys Bicep templates including Key Vault creation
3. **Verify Deployment**: Confirms successful deployment and outputs Key Vault name and application URLs

**Important**: After deployment, you must manually set the required secrets in Key Vault before the application will function properly.

## Key Vault Naming

The Key Vault uses a deterministic naming scheme:
```
kv-green-squirrel-{uniqueString(resourceGroupId)}
```

This ensures the same Key Vault name is used across deployments while remaining globally unique.

## Permissions

The Static Web App's managed identity is automatically granted the **Key Vault Secrets User** role, allowing it to:
- Read secrets from the Key Vault at runtime
- Use Key Vault references in application settings

The deployment service principal also receives this role to enable infrastructure deployment.

## Setting Secrets in Key Vault

### Required Secrets

You must set these three secrets in Key Vault after deployment:

1. **google-client-id**: Google OAuth 2.0 client ID
2. **google-client-secret**: Google OAuth 2.0 client secret
3. **jwt-secret**: Secret key for JWT signing (minimum 32 characters)

### Using Azure Portal

1. Navigate to Azure Portal (https://portal.azure.com)
2. Go to your Key Vault (e.g., `kv-green-squirrel-xxxxxxxxxxxx`)
3. Click **Secrets** in the left menu
4. Click **+ Generate/Import**
5. Enter the secret name and value
6. Click **Create**

### Using Azure CLI

```bash
# Login to Azure
az login

# Get Key Vault name from deployment output
KV_NAME=$(az deployment group show \
  --resource-group rg-green-squirrel \
  --name main \
  --query properties.outputs.keyVaultName.value \
  --output tsv)

# Set Google Client ID
az keyvault secret set \
  --vault-name "$KV_NAME" \
  --name "google-client-id" \
  --value "YOUR_GOOGLE_CLIENT_ID"

# Set Google Client Secret
az keyvault secret set \
  --vault-name "$KV_NAME" \
  --name "google-client-secret" \
  --value "YOUR_GOOGLE_CLIENT_SECRET"

# Set JWT Secret (generate a secure random string)
az keyvault secret set \
  --vault-name "$KV_NAME" \
  --name "jwt-secret" \
  --value "YOUR_SECURE_JWT_SECRET_MIN_32_CHARS"

# Verify secrets are set
az keyvault secret list \
  --vault-name "$KV_NAME" \
  --query "[].name" \
  --output tsv
```

## Security Best Practices

1. **RBAC**: The Key Vault uses Azure RBAC for access control instead of access policies
2. **Network Access**: Public network access is enabled with Azure Services bypass for deployment
3. **Secrets Rotation**: Implement a process to rotate secrets regularly
4. **Audit Logging**: Key Vault operations are logged to Application Insights

## Troubleshooting

### Secret Not Found Error

If deployment fails with "secret not found":
1. Verify the secret exists in Key Vault: `az keyvault secret list --vault-name <name>`
2. Check the secret name matches exactly (case-sensitive)
3. Ensure the service principal has proper permissions

### Key Vault Access Denied

If you get access denied errors:
1. Verify the service principal has the "Key Vault Secrets User" role
2. Check that RBAC is enabled on the Key Vault
3. Ensure the correct object ID is being passed to the deployment

### Key Vault Name Too Long

Key Vault names must be 3-24 characters. If you encounter issues:
1. The generated name should always fit this constraint
2. Check for any custom naming overrides that might be too long
