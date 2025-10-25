#!/bin/bash

# Check Aspire AppHost status

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PID_FILE="$PROJECT_ROOT/aspire.pid"

echo "=== Aspire AppHost Status ==="

if [ ! -f "$PID_FILE" ]; then
    echo "❌ Aspire is not running (no PID file)"
    exit 1
fi

ASPIRE_PID=$(cat "$PID_FILE")

if ! ps -p "$ASPIRE_PID" > /dev/null 2>&1; then
    echo "❌ Aspire process not found (PID $ASPIRE_PID is stale)"
    exit 1
fi

echo "✅ Aspire is running (PID: $ASPIRE_PID)"
echo ""

# Check services
echo "Checking services..."

check_service() {
    local name=$1
    local url=$2
    
    if curl -s "$url" > /dev/null 2>&1; then
        echo "  ✅ $name: $url"
        return 0
    else
        echo "  ❌ $name: $url (not responding)"
        return 1
    fi
}

check_service "Aspire Dashboard" "http://localhost:17126"
check_service "Web API" "http://localhost:5103/health"
check_service "Dashboard" "http://localhost:5183"
check_service "Student App" "http://localhost:5049"

echo ""
echo "PostgreSQL: localhost:5432"
echo "Redis: localhost:6379"
echo "Ollama: http://localhost:11434"
