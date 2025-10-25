#!/bin/bash

# Start Aspire AppHost in the background using nohup
# This keeps the service running independently of the terminal

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
LOG_FILE="$PROJECT_ROOT/aspire.log"
PID_FILE="$PROJECT_ROOT/aspire.pid"

echo "=== Starting Aspire AppHost ==="
echo "Project root: $PROJECT_ROOT"
echo "Log file: $LOG_FILE"
echo "PID file: $PID_FILE"

# Check if already running
if [ -f "$PID_FILE" ]; then
    OLD_PID=$(cat "$PID_FILE")
    if ps -p "$OLD_PID" > /dev/null 2>&1; then
        echo "Aspire is already running with PID $OLD_PID"
        echo "Use scripts/stop-aspire.sh to stop it first"
        exit 1
    else
        echo "Removing stale PID file"
        rm -f "$PID_FILE"
    fi
fi

# Start Aspire in the background
cd "$PROJECT_ROOT"
nohup dotnet run --project src/EduMind.AppHost > "$LOG_FILE" 2>&1 &
ASPIRE_PID=$!

# Save PID
echo $ASPIRE_PID > "$PID_FILE"

echo "Aspire started with PID: $ASPIRE_PID"
echo "Waiting for services to start..."

# Wait for Aspire dashboard to be ready
MAX_WAIT=180
WAIT_COUNT=0
while [ $WAIT_COUNT -lt $MAX_WAIT ]; do
    if curl -k -s https://localhost:17126 > /dev/null 2>&1; then
        echo ""
        echo "✅ Aspire Dashboard is ready!"
        
        # Extract login token from logs
        if [ -f "$LOG_FILE" ]; then
            TOKEN=$(grep -oP 'login\?t=\K[a-f0-9]+' "$LOG_FILE" | tail -1)
            if [ -n "$TOKEN" ]; then
                echo ""
                echo "🔐 Login URL: https://localhost:17126/login?t=$TOKEN"
            fi
        fi
        echo "   Dashboard: https://localhost:17126"
        break
    fi
    echo -n "."
    sleep 1
    WAIT_COUNT=$((WAIT_COUNT + 1))
done

if [ $WAIT_COUNT -eq $MAX_WAIT ]; then
    echo ""
    echo "⚠️  Timeout waiting for Aspire to start"
    echo "   Check logs: tail -f $LOG_FILE"
    exit 1
fi

# Wait a bit more for all services to start
echo "Waiting for services to initialize..."
sleep 15

# Check Web API health
echo ""
echo "Checking service health..."
if curl -s http://localhost:5103/health > /dev/null 2>&1; then
    echo "✅ Web API is healthy"
    echo "   Web API: http://localhost:5103"
else
    echo "⚠️  Web API not responding yet (may still be starting)"
fi

# Check Student App (give it extra time to warm up - Blazor takes a while on first start)
echo ""
echo "Warming up Student App (this can take 1-2 minutes on first start)..."
MAX_WAIT=90
WAIT_COUNT=0
while [ $WAIT_COUNT -lt $MAX_WAIT ]; do
    if curl -s http://localhost:5049 > /dev/null 2>&1; then
        echo "✅ Student App is ready"
        echo "   Student App: http://localhost:5049"
        echo ""
        echo "   Note: First page load may take an additional 30 seconds for Blazor compilation"
        break
    fi
    if [ $((WAIT_COUNT % 10)) -eq 0 ]; then
        echo "   Still waiting... ($WAIT_COUNT seconds)"
    fi
    sleep 1
    WAIT_COUNT=$((WAIT_COUNT + 1))
done

if [ $WAIT_COUNT -eq $MAX_WAIT ]; then
    echo "⚠️  Student App not responding yet"
    echo "   It may still be starting - try accessing in 1-2 minutes: http://localhost:5049"
fi

echo ""
echo "=== Aspire is running ==="
echo "Services:"
echo "  - Aspire Dashboard: https://localhost:17126"
echo "  - Web API: http://localhost:5103"
echo "  - Dashboard: http://localhost:5183"
echo "  - 🎓 Student App: http://localhost:5049"
echo ""
echo "⏱️  First-time startup can take 1-2 minutes"
echo "   If you get 'connection refused', wait 30-60 seconds and try again"
echo ""
echo "Logs: tail -f $LOG_FILE"
echo "Stop: ./scripts/stop-aspire.sh"
echo "Status: ./scripts/status-aspire.sh"
