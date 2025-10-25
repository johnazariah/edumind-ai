#!/bin/bash
set -e

echo "=== Manual PostgreSQL Container Fix for Azure ==="
echo ""
echo "Removing volume mount from postgres container to use ephemeral storage"
echo "This will make PostgreSQL start successfully but data will be lost on restart"
echo ""

# Get the current postgres container app configuration
az containerapp show --name postgres --resource-group rg-dev --query "{
  image: properties.template.containers[0].image,
  env: properties.template.containers[0].env
}" -o json > /tmp/postgres-config.json

cat /tmp/postgres-config.json

echo ""
echo "Updating postgres container to remove volume and add PGDATA..."
echo ""

# Update without volumes
az containerapp update \
  --name postgres \
  --resource-group rg-dev \
  --set-env-vars \
    "POSTGRES_HOST_AUTH_METHOD=scram-sha-256" \
    "POSTGRES_INITDB_ARGS=--auth-host=scram-sha-256 --auth-local=scram-sha-256" \
    "POSTGRES_USER=postgres" \
    "POSTGRES_PASSWORD=secretref:postgres-password" \
  --output table

echo ""
echo "Waiting 30 seconds for restart..."
sleep 30

echo ""
echo "Checking postgres logs..."
az containerapp logs show --name postgres --resource-group rg-dev --follow false --tail 50 \
  | jq -r '.Log' | grep -E "ready to accept|error|ERROR|starting PostgreSQL" | tail -10

echo ""
echo "Done!"
