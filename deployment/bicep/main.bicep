// Main Bicep template for EduMind.AI Azure infrastructure
// This deploys all required Azure resources for production

@description('The environment name (dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environmentName string = 'dev'

@description('The primary Azure region for resources')
param location string = resourceGroup().location

@description('The base name for all resources')
param baseName string = 'edumind'

@description('Administrator email for alerts and notifications')
param adminEmail string

@description('Enable Azure AD B2C configuration')
param enableB2C bool = true

@description('Google OAuth Client ID (from Google Cloud Console)')
@secure()
param googleClientId string = ''

@description('Google OAuth Client Secret (from Google Cloud Console)')
@secure()
param googleClientSecret string = ''

// Variables
var resourcePrefix = '${baseName}-${environmentName}'
var tags = {
  Environment: environmentName
  Application: 'EduMind.AI'
  ManagedBy: 'Bicep'
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${resourcePrefix}-plan'
  location: location
  tags: tags
  sku: {
    name: environmentName == 'prod' ? 'P1v3' : 'B1'
    tier: environmentName == 'prod' ? 'PremiumV3' : 'Basic'
    capacity: environmentName == 'prod' ? 2 : 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// Web API App Service
resource webApiApp 'Microsoft.Web/sites@2023-01-01' = {
  name: '${resourcePrefix}-api'
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: environmentName == 'prod'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName == 'prod' ? 'Production' : 'Staging'
        }
        {
          name: 'ConnectionStrings__AcademicDatabase'
          value: 'Host=${postgresServer.properties.fullyQualifiedDomainName};Database=edumind;Username=${postgresAdminLogin};Password=${postgresAdminPassword};SslMode=Require'
        }
        {
          name: 'ConnectionStrings__RedisCache'
          value: '${redisCache.properties.hostName}:${redisCache.properties.sslPort},password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
        }
        {
          name: 'AzureAdB2C__Instance'
          value: 'https://login.microsoftonline.com/'
        }
        {
          name: 'AzureAdB2C__Domain'
          value: 'edumindai.onmicrosoft.com'
        }
        {
          name: 'AzureAdB2C__TenantId'
          value: enableB2C ? b2cTenantId : ''
        }
        {
          name: 'AzureAdB2C__ClientId'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/AzureAdB2C-ClientId/)'
        }
        {
          name: 'AzureAdB2C__ClientSecret'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/AzureAdB2C-ClientSecret/)'
        }
        {
          name: 'AzureAdB2C__SignUpSignInPolicyId'
          value: 'B2C_1_susi_google'
        }
        {
          name: 'Authentication__Enabled'
          value: 'true'
        }
        {
          name: 'Ollama__BaseUrl'
          value: 'http://localhost:11434'
        }
        {
          name: 'LLM__Provider'
          value: 'AzureOpenAI'
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: appInsights.properties.ConnectionString
        }
      ]
    }
  }
}

// PostgreSQL Flexible Server
@secure()
param postgresAdminLogin string = 'edumind_admin'

@secure()
param postgresAdminPassword string

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: '${resourcePrefix}-postgres'
  location: location
  tags: tags
  sku: {
    name: environmentName == 'prod' ? 'Standard_D2ds_v4' : 'Standard_B1ms'
    tier: environmentName == 'prod' ? 'GeneralPurpose' : 'Burstable'
  }
  properties: {
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    version: '16'
    storage: {
      storageSizeGB: environmentName == 'prod' ? 128 : 32
    }
    backup: {
      backupRetentionDays: environmentName == 'prod' ? 30 : 7
      geoRedundantBackup: environmentName == 'prod' ? 'Enabled' : 'Disabled'
    }
    highAvailability: {
      mode: environmentName == 'prod' ? 'ZoneRedundant' : 'Disabled'
    }
  }
}

// PostgreSQL Database
resource postgresDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
  parent: postgresServer
  name: 'edumind'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// PostgreSQL Firewall Rule - Allow Azure Services
resource postgresFirewallAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
  parent: postgresServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Redis Cache
resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: '${resourcePrefix}-redis'
  location: location
  tags: tags
  properties: {
    sku: {
      name: environmentName == 'prod' ? 'Premium' : 'Basic'
      family: environmentName == 'prod' ? 'P' : 'C'
      capacity: environmentName == 'prod' ? 1 : 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

// Key Vault for secrets
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${resourcePrefix}-kv'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Grant Key Vault access to Web App
resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, webApiApp.id, 'Key Vault Secrets User')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: webApiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${resourcePrefix}-insights'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${resourcePrefix}-logs'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: environmentName == 'prod' ? 90 : 30
  }
}

// Placeholder for B2C Tenant ID (must be created separately)
@description('Azure AD B2C Tenant ID (create tenant first)')
param b2cTenantId string = ''

// Outputs
output webApiUrl string = 'https://${webApiApp.properties.defaultHostName}'
output postgresServerName string = postgresServer.name
output postgresServerFqdn string = postgresServer.properties.fullyQualifiedDomainName
output redisCacheName string = redisCache.name
output keyVaultName string = keyVault.name
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output webAppIdentityPrincipalId string = webApiApp.identity.principalId
