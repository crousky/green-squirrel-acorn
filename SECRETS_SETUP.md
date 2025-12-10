# Quick Start: Setting Up Secrets

## Overview

After deploying the infrastructure, you need to manually set three secrets in Azure Key Vault for the application to function.

## Step 1: Get Your Key Vault Name

After running the deployment workflow, note the Key Vault name from the GitHub Actions output, or retrieve it using:

```bash
az deployment group show \
  --resource-group rg-green-squirrel \
  --name main \
  --query properties.outputs.keyVaultName.value \
  --output tsv
```

## Step 2: Set Required Secrets

### Option A: Azure Portal (Easiest)

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Key Vault
3. Click **Secrets** → **+ Generate/Import**
4. Create these three secrets:

| Secret Name | Description | Example Value |
|------------|-------------|---------------|
| `google-client-id` | Google OAuth 2.0 Client ID | `123456789-abc.apps.googleusercontent.com` |
| `google-client-secret` | Google OAuth 2.0 Client Secret | `GOCSPX-abc123...` |
| `jwt-secret` | JWT signing key (min 32 chars) | Generate using method below |

### Option B: Azure CLI

```bash
# Set your Key Vault name
KV_NAME="kv-green-squirrel-xxxxxxxxxxxxx"

# Set secrets
az keyvault secret set --vault-name "$KV_NAME" --name "google-client-id" --value "YOUR_VALUE"
az keyvault secret set --vault-name "$KV_NAME" --name "google-client-secret" --value "YOUR_VALUE"
az keyvault secret set --vault-name "$KV_NAME" --name "jwt-secret" --value "YOUR_VALUE"
```

## Step 3: Generate a Secure JWT Secret

### PowerShell
```powershell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

### Bash/Linux
```bash
openssl rand -base64 48
```

### Online (Not Recommended for Production)
Use a password generator to create a 64+ character random string.

## Step 4: Verify Setup

Check that all secrets are set:

```bash
az keyvault secret list \
  --vault-name "$KV_NAME" \
  --query "[].{Name:name, Enabled:attributes.enabled}" \
  --output table
```

You should see:
- ✓ google-client-id
- ✓ google-client-secret
- ✓ jwt-secret

## Step 5: Restart Static Web App (if needed)

If the app was already deployed, you may need to restart it for the changes to take effect:

```bash
az staticwebapp restart \
  --name green-squirrel-site \
  --resource-group rg-green-squirrel
```

## How It Works

The Static Web App uses **Key Vault references** in its application settings:

```
Google__ClientId: @Microsoft.KeyVault(VaultName=kv-xxx;SecretName=google-client-id)
```

This means:
- Secrets are never exposed in configuration files
- The app retrieves secrets directly from Key Vault at runtime
- You can rotate secrets in Key Vault without redeploying code

## Troubleshooting

### "Access Denied" when setting secrets

You need **Key Vault Secrets Officer** role or **Contributor** access to the Key Vault. Contact your Azure administrator.

### App returns authentication errors

1. Verify all three secrets are set correctly
2. Check secret names match exactly (case-sensitive)
3. Verify the Static Web App has the **Key Vault Secrets User** role
4. Restart the Static Web App

### How to view/update a secret

```bash
# View secret value
az keyvault secret show \
  --vault-name "$KV_NAME" \
  --name "google-client-id" \
  --query value \
  --output tsv

# Update secret
az keyvault secret set \
  --vault-name "$KV_NAME" \
  --name "google-client-id" \
  --value "NEW_VALUE"
```
