#!/bin/bash
# Azure Secret Injection Script
# Injects PostgreSQL and Redis connection strings directly into Container App secrets
# This bypasses the azd template substitution issue

set -e

RESOURCE_GROUP="${1:-rg-staging}"
POSTGRES_PASSWORD="${2}"

if [ -z "$POSTGRES_PASSWORD" ]; then
    echo "❌ Error: PostgreSQL password required"
    echo ""
    echo "Usage: $0 <resource-group> <postgres-password>"
    echo ""
    echo "Example:"
    echo "  $0 rg-staging 'YourSecurePassword123!'"
    echo ""
    echo "Or with Key Vault:"
    echo "  PASS=\$(az keyvault secret show --name postgres-password --vault-name <vault> --query value -o tsv)"
    echo "  $0 rg-staging \"\$PASS\""
    exit 1
fi

echo "=========================================="
echo "🔧 Azure Secret Injection"
echo "=========================================="
echo "Resource Group: $RESOURCE_GROUP"
echo ""

# Check if resource group exists
if ! az group show --name "$RESOURCE_GROUP" &> /dev/null; then
    echo "❌ Error: Resource group '$RESOURCE_GROUP' not found"
    exit 1
fi

echo "✅ Resource group exists"

# Get PostgreSQL FQDN
echo "📊 Retrieving PostgreSQL server information..."
POSTGRES_HOST=$(az postgres flexible-server list \
    --resource-group "$RESOURCE_GROUP" \
    --query "[0].fullyQualifiedDomainName" -o tsv 2>/dev/null || echo "")

if [ -z "$POSTGRES_HOST" ]; then
    echo "❌ Error: No PostgreSQL Flexible Server found in resource group"
    exit 1
fi

echo "   PostgreSQL Host: $POSTGRES_HOST"

# Get Redis host (assuming internal DNS pattern)
REDIS_HOSTNAME=$(az containerapp env list \
    --resource-group "$RESOURCE_GROUP" \
    --query "[0].name" -o tsv 2>/dev/null || echo "")

if [ -z "$REDIS_HOSTNAME" ]; then
    echo "⚠️  Warning: Could not determine Container App Environment name"
    echo "   Redis connection string will use 'cache' hostname"
    REDIS_CONNECTION="cache:6379"
else
    # Try to get the internal domain from existing container app
    INTERNAL_DOMAIN=$(az containerapp show \
        --name webapi \
        --resource-group "$RESOURCE_GROUP" \
        --query "properties.configuration.ingress.fqdn" -o tsv 2>/dev/null | \
        sed 's/webapi\.//' || echo "")
    
    if [ -n "$INTERNAL_DOMAIN" ]; then
        REDIS_CONNECTION="cache.internal.${INTERNAL_DOMAIN}:6379"
        echo "   Redis Host: cache.internal.${INTERNAL_DOMAIN}"
    else
        REDIS_CONNECTION="cache:6379"
        echo "   Redis Host: cache (short hostname - will be patched at runtime)"
    fi
fi

# Construct PostgreSQL connection string
CONNECTION_STRING="Host=${POSTGRES_HOST};Port=5432;Username=edumind_admin;Password=${POSTGRES_PASSWORD};Database=edumind;SslMode=Require"

echo ""
echo "💉 Injecting connection strings..."

# Check if container app exists
if ! az containerapp show --name webapi --resource-group "$RESOURCE_GROUP" &> /dev/null; then
    echo "❌ Error: Container App 'webapi' not found in resource group"
    exit 1
fi

# Inject PostgreSQL connection string as secret
echo "   Setting PostgreSQL secret..."
az containerapp secret set \
    --name webapi \
    --resource-group "$RESOURCE_GROUP" \
    --secrets "connectionstrings--edumind=${CONNECTION_STRING}" \
    --output none 2>/dev/null || {
        echo "❌ Error: Failed to set PostgreSQL secret"
        exit 1
    }

# Inject Redis connection string as secret
echo "   Setting Redis secret..."
az containerapp secret set \
    --name webapi \
    --resource-group "$RESOURCE_GROUP" \
    --secrets "connectionstrings--redis=${REDIS_CONNECTION}" \
    --output none 2>/dev/null || {
        echo "❌ Error: Failed to set Redis secret"
        exit 1
    }

# Update environment variables to use secrets
echo "   Updating environment variables..."
az containerapp update \
    --name webapi \
    --resource-group "$RESOURCE_GROUP" \
    --set-env-vars \
        "ConnectionStrings__DefaultConnection=secretref:connectionstrings--edumind" \
        "ConnectionStrings__Redis=secretref:connectionstrings--redis" \
    --output none 2>/dev/null || {
        echo "❌ Error: Failed to update environment variables"
        exit 1
    }

echo ""
echo "✅ Connection strings injected successfully!"
echo ""
echo "🔄 Container App will restart automatically..."
echo "⏳ Please wait ~30-60 seconds for restart to complete"
echo ""

# Get the webapi FQDN
WEBAPI_FQDN=$(az containerapp show \
    --name webapi \
    --resource-group "$RESOURCE_GROUP" \
    --query "properties.configuration.ingress.fqdn" -o tsv 2>/dev/null || echo "")

if [ -n "$WEBAPI_FQDN" ]; then
    echo "🌐 Web API URL: https://${WEBAPI_FQDN}"
    echo "🏥 Health Check: https://${WEBAPI_FQDN}/health"
    echo ""
    echo "Test with:"
    echo "  curl https://${WEBAPI_FQDN}/health"
else
    echo "⚠️  Could not retrieve Web API FQDN"
fi

echo ""
echo "=========================================="
echo "✅ Injection Complete"
echo "=========================================="
