#!/bin/bash

# Stop Aspire AppHost

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PID_FILE="$PROJECT_ROOT/aspire.pid"

echo "=== Stopping Aspire AppHost ==="

if [ ! -f "$PID_FILE" ]; then
    echo "Aspire is not running (no PID file found)"
    exit 0
fi

ASPIRE_PID=$(cat "$PID_FILE")

if ps -p "$ASPIRE_PID" > /dev/null 2>&1; then
    echo "Stopping Aspire (PID: $ASPIRE_PID)..."
    kill "$ASPIRE_PID"
    
    # Wait for process to stop
    for i in {1..10}; do
        if ! ps -p "$ASPIRE_PID" > /dev/null 2>&1; then
            break
        fi
        sleep 1
    done
    
    # Force kill if still running
    if ps -p "$ASPIRE_PID" > /dev/null 2>&1; then
        echo "Force killing Aspire..."
        kill -9 "$ASPIRE_PID"
    fi
    
    echo "✅ Aspire stopped"
else
    echo "Aspire process not found (was PID $ASPIRE_PID)"
fi

rm -f "$PID_FILE"
echo "Cleaned up PID file"
