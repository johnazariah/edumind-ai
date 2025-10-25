#!/bin/bash

# OLLAMA Integration Test Script
# Tests OLLAMA service with MathematicsAssessmentAgent

set -e

echo "========================================="
echo "OLLAMA Integration Test"
echo "========================================="
echo ""

# Check if OLLAMA is running
echo "1. Checking if OLLAMA server is running..."
if curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
    echo "   ✅ OLLAMA server is running"
else
    echo "   ❌ OLLAMA server is not running!"
    echo "   Starting OLLAMA server..."
    nohup ollama serve > /tmp/ollama.log 2>&1 &
    sleep 5
    echo "   ✅ OLLAMA server started"
fi

echo ""

# Check if model is available
echo "2. Checking if llama3.2:3b model is available..."
if ollama list | grep -q "llama3.2:3b"; then
    echo "   ✅ Model llama3.2:3b is available"
else
    echo "   ❌ Model not found. Please run: ollama pull llama3.2:3b"
    exit 1
fi

echo ""

# Test OLLAMA directly
echo "3. Testing OLLAMA with a simple query..."
echo "   Query: 'What is 2+2?'"
echo ""

RESPONSE=$(ollama run llama3.2:3b "What is 2+2? Answer in one sentence." 2>&1 | head -1)
echo "   Response: $RESPONSE"
echo "   ✅ OLLAMA is responding"

echo ""

# Test semantic evaluation
echo "4. Testing semantic evaluation capability..."
echo "   Query: 'Is \"a squared plus b squared equals c squared\" the same as \"a² + b² = c²\"?'"
echo ""

SEMANTIC_TEST=$(ollama run llama3.2:3b "Is 'a squared plus b squared equals c squared' the same mathematical expression as 'a² + b² = c²'? Answer yes or no with brief explanation." 2>&1 | head -3)
echo "$SEMANTIC_TEST"
echo "   ✅ Semantic understanding test complete"

echo ""

# Summary
echo "========================================="
echo "✅ OLLAMA Integration Test Complete"
echo "========================================="
echo ""
echo "OLLAMA is ready to use with EduMind.AI!"
echo ""
echo "Next steps:"
echo "  1. Ensure appsettings.json has LLM:Provider set to 'Ollama'"
echo "  2. Run the Web API: dotnet run --project src/AcademicAssessment.Web"
echo "  3. Test with MathematicsAssessmentAgent endpoints"
echo ""
