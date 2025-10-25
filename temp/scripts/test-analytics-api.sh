#!/bin/bash
# EduMind.AI Student Analytics API Test Script
# This script tests all 7 analytics endpoints

BASE_URL="http://localhost:5103"
STUDENT_ID="00000000-0000-0000-0000-000000000001"

echo "=========================================="
echo "EduMind.AI Analytics API Test"
echo "=========================================="
echo ""

# Test 1: Health Check
echo "1. Testing Health Endpoint..."
curl -s "$BASE_URL/health" | jq '.status'
echo ""

# Test 2: Performance Summary
echo "2. Testing Performance Summary..."
curl -s "$BASE_URL/api/v1/students/$STUDENT_ID/analytics/performance-summary" | jq '{studentId, totalAssessmentsTaken, averageScore, overallMastery}'
echo ""

# Test 3: Subject Performance
echo "3. Testing Subject Performance (Mathematics)..."
curl -s "$BASE_URL/api/v1/students/$STUDENT_ID/analytics/subject-performance?subject=Mathematics" | jq '{subject, assessmentsTaken, averageScore, masteryLevel}'
echo ""

# Test 4: Learning Objectives
echo "4. Testing Learning Objectives..."
curl -s "$BASE_URL/api/v1/students/$STUDENT_ID/analytics/learning-objectives" | jq 'length'
echo ""

# Test 5: Ability Estimates
echo "5. Testing Ability Estimates..."
curl -s "$BASE_URL/api/v1/students/$STUDENT_ID/analytics/ability-estimates" | jq '.'
echo ""

# Test 6: Improvement Areas
echo "6. Testing Improvement Areas (top 5)..."
curl -s "$BASE_URL/api/v1/students/$STUDENT_ID/analytics/improvement-areas?topN=5" | jq 'length'
echo ""

# Test 7: Progress Timeline
echo "7. Testing Progress Timeline (2025)..."
curl -s "$BASE_URL/api/v1/students/$STUDENT_ID/analytics/progress-timeline?startDate=2025-01-01&endDate=2025-12-31" | jq '{studentId, startDate, endDate, dataPointsCount: (.dataPoints | length)}'
echo ""

# Test 8: Peer Comparison
echo "8. Testing Peer Comparison (Grade 9, Mathematics)..."
curl -s "$BASE_URL/api/v1/students/$STUDENT_ID/analytics/peer-comparison?gradeLevel=9&subject=Mathematics" | jq '{studentId, studentScore, peerAverageScore, percentile, gradeLevel}'
echo ""

echo "=========================================="
echo "Validation Tests"
echo "=========================================="
echo ""

# Validation Test 1: Invalid topN
echo "9. Testing Validation - Invalid topN (25)..."
curl -s "$BASE_URL/api/v1/students/$STUDENT_ID/analytics/improvement-areas?topN=25" | jq '.'
echo ""

# Validation Test 2: Invalid date range
echo "10. Testing Validation - Invalid date range..."
curl -s "$BASE_URL/api/v1/students/$STUDENT_ID/analytics/progress-timeline?startDate=2025-12-31&endDate=2025-01-01" | jq '.'
echo ""

echo "=========================================="
echo "All tests completed!"
echo "=========================================="
