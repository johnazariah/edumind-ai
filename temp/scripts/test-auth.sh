#!/bin/bash

# Azure AD B2C Authentication Test Script
# This script helps test JWT authentication after Azure AD B2C setup

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "================================================"
echo "Azure AD B2C Authentication Test"
echo "================================================"
echo ""

# Check if required tools are installed
command -v jq >/dev/null 2>&1 || { echo -e "${RED}Error: jq is required but not installed. Install with: apt-get install jq${NC}"; exit 1; }
command -v curl >/dev/null 2>&1 || { echo -e "${RED}Error: curl is required but not installed.${NC}"; exit 1; }

# Configuration
B2C_TENANT_NAME="${B2C_TENANT_NAME:-edumindai}"
B2C_DOMAIN="${B2C_DOMAIN:-${B2C_TENANT_NAME}.onmicrosoft.com}"
B2C_POLICY="${B2C_POLICY:-B2C_1_susi_google}"
B2C_CLIENT_ID="${B2C_CLIENT_ID}"
B2C_CLIENT_SECRET="${B2C_CLIENT_SECRET}"
API_BASE_URL="${API_BASE_URL:-https://localhost:5001}"
STUDENT_ID="${STUDENT_ID:-00000000-0000-0000-0000-000000000001}"

# Check if required environment variables are set
if [ -z "$B2C_CLIENT_ID" ]; then
    echo -e "${YELLOW}B2C_CLIENT_ID not set. Please set it:${NC}"
    echo "export B2C_CLIENT_ID='your-client-id'"
    echo ""
    read -p "Enter Client ID: " B2C_CLIENT_ID
    export B2C_CLIENT_ID
fi

if [ -z "$B2C_CLIENT_SECRET" ]; then
    echo -e "${YELLOW}B2C_CLIENT_SECRET not set. Please set it:${NC}"
    echo "export B2C_CLIENT_SECRET='your-client-secret'"
    echo ""
    read -s -p "Enter Client Secret: " B2C_CLIENT_SECRET
    export B2C_CLIENT_SECRET
    echo ""
fi

echo -e "${GREEN}Configuration:${NC}"
echo "  Tenant: $B2C_TENANT_NAME"
echo "  Domain: $B2C_DOMAIN"
echo "  Policy: $B2C_POLICY"
echo "  Client ID: ${B2C_CLIENT_ID:0:8}..."
echo "  API URL: $API_BASE_URL"
echo ""

# Generate authorization URL
AUTH_URL="https://${B2C_TENANT_NAME}.b2clogin.com/${B2C_DOMAIN}/${B2C_POLICY}/oauth2/v2.0/authorize"
TOKEN_URL="https://${B2C_TENANT_NAME}.b2clogin.com/${B2C_DOMAIN}/${B2C_POLICY}/oauth2/v2.0/token"
REDIRECT_URI="https://localhost:5001/signin-oidc"

echo -e "${YELLOW}Step 1: Get Authorization Code${NC}"
echo "You need to manually obtain an authorization code by visiting:"
echo ""
echo "$AUTH_URL?client_id=$B2C_CLIENT_ID&response_type=code&redirect_uri=$REDIRECT_URI&response_mode=query&scope=openid%20profile%20email&state=12345"
echo ""
echo "After signing in, you'll be redirected to a URL like:"
echo "https://localhost:5001/signin-oidc?code=AUTHORIZATION_CODE&state=12345"
echo ""
read -p "Enter the authorization code from the URL: " AUTH_CODE

if [ -z "$AUTH_CODE" ]; then
    echo -e "${RED}Error: Authorization code is required${NC}"
    exit 1
fi

echo ""
echo -e "${YELLOW}Step 2: Exchange Code for Access Token${NC}"

TOKEN_RESPONSE=$(curl -s -X POST "$TOKEN_URL" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code" \
  -d "client_id=$B2C_CLIENT_ID" \
  -d "client_secret=$B2C_CLIENT_SECRET" \
  -d "code=$AUTH_CODE" \
  -d "redirect_uri=$REDIRECT_URI" \
  -d "scope=openid profile email")

# Check if token was obtained successfully
if echo "$TOKEN_RESPONSE" | jq -e '.access_token' > /dev/null 2>&1; then
    ACCESS_TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token')
    ID_TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.id_token')
    echo -e "${GREEN}✓ Token obtained successfully${NC}"
    echo ""
    
    # Decode and display ID token claims
    echo -e "${YELLOW}Step 3: Decode JWT Token${NC}"
    echo "ID Token Claims:"
    echo "$ID_TOKEN" | cut -d'.' -f2 | base64 -d 2>/dev/null | jq '.' || echo "Unable to decode token"
    echo ""
    
    # Save token to file
    echo "$ACCESS_TOKEN" > .jwt_token
    echo -e "${GREEN}✓ Token saved to .jwt_token${NC}"
    echo ""
    
else
    echo -e "${RED}✗ Failed to obtain token${NC}"
    echo "Response:"
    echo "$TOKEN_RESPONSE" | jq '.'
    exit 1
fi

echo -e "${YELLOW}Step 4: Test API Endpoints${NC}"
echo ""

# Test 1: Performance Summary
echo "Test 1: GET /api/v1/students/{id}/analytics/performance-summary"
RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X GET \
  "${API_BASE_URL}/api/v1/students/${STUDENT_ID}/analytics/performance-summary" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "accept: application/json" \
  --insecure)

HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS:" | cut -d':' -f2)
BODY=$(echo "$RESPONSE" | sed '/HTTP_STATUS:/d')

if [ "$HTTP_STATUS" = "200" ]; then
    echo -e "${GREEN}✓ Success (200 OK)${NC}"
    echo "$BODY" | jq '.' 2>/dev/null || echo "$BODY"
elif [ "$HTTP_STATUS" = "401" ]; then
    echo -e "${RED}✗ Unauthorized (401) - Token may be invalid${NC}"
elif [ "$HTTP_STATUS" = "403" ]; then
    echo -e "${YELLOW}⚠ Forbidden (403) - User doesn't have access to this student${NC}"
elif [ "$HTTP_STATUS" = "404" ]; then
    echo -e "${YELLOW}⚠ Not Found (404) - Student not found or no data${NC}"
else
    echo -e "${RED}✗ Unexpected response (${HTTP_STATUS})${NC}"
    echo "$BODY"
fi
echo ""

# Test 2: Subject Performance
echo "Test 2: GET /api/v1/students/{id}/analytics/subject-performance"
RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X GET \
  "${API_BASE_URL}/api/v1/students/${STUDENT_ID}/analytics/subject-performance?subject=Mathematics" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "accept: application/json" \
  --insecure)

HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS:" | cut -d':' -f2)

if [ "$HTTP_STATUS" = "200" ]; then
    echo -e "${GREEN}✓ Success (200 OK)${NC}"
elif [ "$HTTP_STATUS" = "401" ]; then
    echo -e "${RED}✗ Unauthorized (401)${NC}"
elif [ "$HTTP_STATUS" = "403" ]; then
    echo -e "${YELLOW}⚠ Forbidden (403)${NC}"
elif [ "$HTTP_STATUS" = "404" ]; then
    echo -e "${YELLOW}⚠ Not Found (404)${NC}"
else
    echo -e "${RED}✗ Unexpected response (${HTTP_STATUS})${NC}"
fi
echo ""

# Test 3: Test without authentication
echo "Test 3: Access without authentication (should fail)"
RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X GET \
  "${API_BASE_URL}/api/v1/students/${STUDENT_ID}/analytics/performance-summary" \
  -H "accept: application/json" \
  --insecure)

HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS:" | cut -d':' -f2)

if [ "$HTTP_STATUS" = "401" ]; then
    echo -e "${GREEN}✓ Correctly rejected (401 Unauthorized)${NC}"
else
    echo -e "${RED}✗ Unexpected response (${HTTP_STATUS}) - should be 401${NC}"
fi
echo ""

echo "================================================"
echo -e "${GREEN}Testing Complete!${NC}"
echo "================================================"
echo ""
echo "Summary:"
echo "  Access Token: ${ACCESS_TOKEN:0:50}..."
echo "  Token saved to: .jwt_token"
echo ""
echo "You can now use this token for manual testing:"
echo "  export JWT_TOKEN=\$(cat .jwt_token)"
echo "  curl -H \"Authorization: Bearer \$JWT_TOKEN\" $API_BASE_URL/api/v1/students/$STUDENT_ID/analytics/performance-summary"
echo ""
