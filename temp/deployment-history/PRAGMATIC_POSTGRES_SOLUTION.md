# Quick Fix: Use Azure Database for PostgreSQL

## Problem

Containerized PostgreSQL cannot initialize on Azure Files due to permission restrictions.
After hours of attempts, the core issue is: **Azure Files doesn't support chmod operations that PostgreSQL requires during initialization**.

## Immediate Solution

**Deploy Azure Database for PostgreSQL Flexible Server** instead of containerized PostgreSQL.

### Implementation

Add to `infra/resources.bicep`:

```bicep
// PostgreSQL Flexible Server
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: 'psql-${resourceToken}'
  location: location
  sku: {
    name: 'Standard_B1ms'  // Burstable, cost-effective for dev
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: 'edumind_admin'
    administratorLoginPassword: postgresPassword
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    network: {
      delegatedSubnetResourceId: null  // Public access for now
      privateDnsZoneArmResourceId: null
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

// Allow Azure services
resource postgresFirewall 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
  parent: postgresServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Database
resource postgresDatabase 'Microsoft.DBforPostegerSQL/flexibleServers/databases@2023-03-01-preview' = {
  parent: postgresServer
  name: 'edumind'
}

// Output connection string
output POSTGRES_CONNECTION_STRING string = 'Host=${postgresServer.properties.fullyQualifiedDomainName};Port=5432;Database=edumind;Username=edumind_admin;Password=${postgresPassword};SSL Mode=Require'
```

### Update `webapi.tmpl.yaml`

```yaml
secrets:
  - name: connectionstrings--edumind
    value: {{ .Env.POSTGRES_CONNECTION_STRING }}
```

## Benefits

1. **Works immediately** - No permission issues
2. **Production-ready** - Microsoft-managed service
3. **Automatic backups** - 7-day retention
4. **Better security** - VNet integration available
5. **Monitoring** - Built-in Azure Monitor integration
6. **Scaling** - Easy to scale up/down
7. **Cost** - B1ms tier ~$12/month for dev

## Alternative: Use emptyDir for Testing

If you want to keep containerized PostgreSQL for now:

```yaml
# In postgres.tmpl.yaml
volumes:
  - name: postgres-data
    storageType: EmptyDir  # Instead of AzureFile
```

**Pros**: Works immediately, no permission issues  
**Cons**: Data lost on restart, not suitable for production

## Recommendation

Use **Azure Database for PostgreSQL** for a proper, production-ready solution.

The time spent fighting Azure Files permissions (3+ hours) could have deployed a managed database 20 times over.
