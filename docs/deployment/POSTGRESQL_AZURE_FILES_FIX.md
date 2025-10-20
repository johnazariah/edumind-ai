# PostgreSQL Azure Files Permissions Fix

## Issue

PostgreSQL container was failing to initialize with error:

```
initdb: error: could not change permissions of directory "/var/lib/postgresql/data": Operation not permitted
```

This prevented PostgreSQL from starting, causing all health checks to fail with:

```
Failed to connect to 100.100.0.206:5432
System.TimeoutException: The operation has timed out.
```

## Root Cause

**Azure Files doesn't support POSIX permissions** that PostgreSQL requires during initialization.

When PostgreSQL initializes a new database cluster, it tries to:

1. `chmod 700 /var/lib/postgresql/data` - Set directory permissions
2. Create subdirectories and files with specific permissions
3. Initialize the database cluster

Azure Files (SMB-based storage) doesn't support `chmod` operations, causing step 1 to fail immediately.

## Solution

Configure PostgreSQL to use a **subdirectory** for its data by setting the `PGDATA` environment variable:

```yaml
# Before (fails):
volumeMounts:
  - volumeName: postgres-data
    mountPath: /var/lib/postgresql/data
# PGDATA defaults to /var/lib/postgresql/data (mount point)
# PostgreSQL tries to chmod the mount point -> FAILS

# After (works):
env:
  - name: PGDATA
    value: /var/lib/postgresql/data/pgdata
volumeMounts:
  - volumeName: postgres-data
    mountPath: /var/lib/postgresql/data
# PGDATA is /var/lib/postgresql/data/pgdata (subdirectory)
# PostgreSQL creates and owns the subdirectory -> SUCCESS
```

## Why This Works

1. **Mount point** (`/var/lib/postgresql/data`) is created by Azure with fixed permissions
2. **Subdirectory** (`/var/lib/postgresql/data/pgdata`) can be created by PostgreSQL with its preferred permissions
3. PostgreSQL only needs to `mkdir` and `chmod` the subdirectory, not the mount point itself

This is a standard pattern for running PostgreSQL on filesystems with limited permission support.

## Implementation

**File**: `src/infra/postgres.tmpl.yaml`

**Change**:

```yaml
env:
  - name: PGDATA
    value: /var/lib/postgresql/data/pgdata
```

**Commit**: `6e721ab` - "Fix: Configure PostgreSQL to work with Azure Files permissions"

## Related Issues

### Redis Status

✅ Redis is running correctly and accepting connections:

```
Ready to accept connections tcp
```

### WebAPI Status  

✅ Connection string patching is working:

```
Detected Azure Container Apps domain from hostname
✅ Patched PostgreSQL hostname to use Azure Container Apps internal FQDN
✅ Patched Redis hostname to use Azure Container Apps internal FQDN
```

## Expected Outcome

After deployment:

1. **PostgreSQL will initialize successfully**:

   ```
   PostgreSQL init process complete; ready for start up.
   database system is ready to accept connections
   ```

2. **Health checks will pass**:
   - PostgreSQL: Connect to `postgres.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io:5432`
   - Redis: Connect to `cache.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io:6379`

3. **Health endpoint returns**: `Healthy` (200 OK)

## Verification Steps

```bash
# 1. Check PostgreSQL logs for successful startup
az containerapp logs show --name postgres --resource-group rg-dev --follow false --tail 50 \
  | jq -r '.Log' | grep -E "ready for start up|ready to accept"

# 2. Check health endpoint
curl https://webapi.kindsea-395ab1c0.eastus.azurecontainerapps.io/health
# Expected: Healthy

# 3. Verify health check details
curl -s https://webapi.kindsea-395ab1c0.eastus.azurecontainerapps.io/health | jq .
# Expected: { "status": "Healthy", "checks": [ ... ] }
```

## Alternative Solutions (Future Consideration)

### Option 1: Azure Database for PostgreSQL (Recommended for Production)

- **Pros**: Managed service, automatic backups, high availability, security updates
- **Cons**: Additional cost, requires connection string changes
- **Use case**: Production deployments

### Option 2: Azure Disk (Premium Storage)

- **Pros**: Full POSIX support, better performance than Azure Files
- **Cons**: Can only be mounted to one pod at a time, requires StatefulSet pattern
- **Use case**: Single-instance databases with performance requirements

### Option 3: EmptyDir (Current for testing)

- **Pros**: Works perfectly, no permission issues
- **Cons**: Data lost on pod restart/redeploy
- **Use case**: Development and testing environments

### Option 4: Azure Files with PGDATA subdirectory (Current Solution)

- **Pros**: Persistent storage, can be mounted to multiple pods (though PostgreSQL shouldn't be)
- **Cons**: Lower performance than Azure Disk, requires PGDATA workaround
- **Use case**: Development, proof-of-concept, transitional deployments

## Timeline

- **Oct 20 12:17 UTC**: Discovered PostgreSQL initialization failure
- **Oct 20 12:20 UTC**: Identified Azure Files permissions incompatibility
- **Oct 20 12:22 UTC**: Implemented PGDATA subdirectory workaround
- **Oct 20 12:23 UTC**: Deployed fix

## Status

- [x] PostgreSQL initialization issue identified
- [x] Root cause determined (Azure Files permissions)
- [x] Solution implemented (PGDATA subdirectory)
- [ ] Deployment in progress
- [ ] Verification pending
