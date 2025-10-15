# Scripts Directory

Helper scripts for EduMind.AI development and testing.

## Available Scripts

### test-auth.sh

Tests Azure AD B2C authentication with Google OAuth 2.0.

**Purpose**: Validates JWT authentication flow and tests API endpoints with real tokens.

**Prerequisites**:

- Azure AD B2C tenant configured
- Google OAuth 2.0 configured
- Application running on <https://localhost:5001>

**Usage**:

```bash
# Set environment variables
export B2C_CLIENT_ID="your-client-id"
export B2C_CLIENT_SECRET="your-client-secret"

# Run the script
./scripts/test-auth.sh
```

**What it does**:

1. Prompts you to sign in with Google via Azure AD B2C
2. Exchanges authorization code for access token
3. Decodes and displays JWT claims
4. Tests 3 API endpoints with the token
5. Verifies authentication is enforced (401 without token)
6. Saves token to `.jwt_token` for manual testing

**Manual testing after running script**:

```bash
# Load token
export JWT_TOKEN=$(cat .jwt_token)

# Test any endpoint
curl -H "Authorization: Bearer $JWT_TOKEN" \
  https://localhost:5001/api/v1/students/00000000-0000-0000-0000-000000000001/analytics/performance-summary
```

## Future Scripts

- `setup-dev-data.sh` - Populate database with test data
- `deploy-staging.sh` - Deploy to Azure staging environment
- `run-load-tests.sh` - Performance testing with k6
- `backup-db.sh` - Backup PostgreSQL database

## Contributing

When adding new scripts:

1. Make them executable: `chmod +x script-name.sh`
2. Add error handling with `set -e`
3. Include usage documentation at the top
4. Add entry to this README
5. Use colors for output (see test-auth.sh for example)
