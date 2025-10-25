# TODO-005: Implement Answer Review Page

**Priority:** P1 - High  
**Area:** Frontend / Blazor  
**Estimated Effort:** Medium (4-5 hours)  
**Status:** Not Started

## Description

Create a detailed answer review page that allows students to review their submitted assessment with correct answers, explanations, and personalized feedback for each question.

## Context

After completing an assessment and viewing results, students need the ability to review their answers to understand mistakes and learn from them. The results page has a "Review Answers" button that currently has no implementation (TODO comment at line 107 of `AssessmentResults.razor.cs`).

This review experience should be:

- Educational, not just showing right/wrong
- Provide explanations for correct answers
- Give personalized feedback for incorrect answers
- Allow students to understand their reasoning errors
- Include links to learning resources

## Technical Requirements

### API Endpoint

```csharp
GET /api/v1.0/Assessment/review/{sessionId}
```

**Response:**

```json
{
  "sessionId": "guid",
  "assessmentTitle": "Introduction to Algebra",
  "submittedAt": "2025-10-25T15:00:00Z",
  "overallScore": 85.0,
  "questions": [
    {
      "questionNumber": 1,
      "questionId": "guid",
      "questionText": "Solve for x: 2x + 5 = 13",
      "questionType": "MultipleChoice",
      "subject": "Algebra",
      "difficulty": "Medium",
      "points": 5,
      "studentAnswer": {
        "selectedOptions": ["x = 4"],
        "freeTextResponse": null,
        "isCorrect": true,
        "pointsEarned": 5
      },
      "correctAnswer": {
        "selectedOptions": ["x = 4"],
        "explanation": "To solve: 2x + 5 = 13, subtract 5 from both sides to get 2x = 8, then divide by 2 to get x = 4."
      },
      "aiFeedback": "Excellent work! You correctly identified the steps to isolate the variable.",
      "learningResources": [
        {
          "title": "Linear Equations Tutorial",
          "url": "https://example.com/linear-equations",
          "type": "Video"
        }
      ]
    },
    {
      "questionNumber": 2,
      "questionId": "guid",
      "questionText": "What is the slope of the line 3x + 2y = 6?",
      "questionType": "ShortAnswer",
      "subject": "Algebra",
      "difficulty": "Hard",
      "points": 10,
      "studentAnswer": {
        "selectedOptions": [],
        "freeTextResponse": "m = 3/2",
        "isCorrect": false,
        "pointsEarned": 0
      },
      "correctAnswer": {
        "selectedOptions": [],
        "freeTextResponse": "m = -3/2",
        "explanation": "Convert to slope-intercept form: 2y = -3x + 6, then y = -3/2 x + 3. The slope is -3/2."
      },
      "aiFeedback": "You correctly identified that the slope comes from rearranging the equation, but forgot the negative sign. Remember that when moving terms, the sign changes.",
      "commonMistakes": [
        "Forgetting to include the negative sign when isolating y"
      ],
      "learningResources": [
        {
          "title": "Slope-Intercept Form Review",
          "url": "https://example.com/slope-intercept",
          "type": "Article"
        }
      ]
    }
  ]
}
```

### UI Components

**1. Review Header**

- Assessment title
- Overall score badge
- Submitted timestamp
- Navigation: "Return to Results" button

**2. Question Review Card** (for each question)

```razor
<div class="question-review-card @(question.IsCorrect ? "correct" : "incorrect")">
    <div class="question-header">
        <span class="question-number">Question @question.QuestionNumber</span>
        <span class="points">@question.PointsEarned / @question.Points points</span>
        <span class="result-badge">
            @if (question.IsCorrect)
            {
                <i class="fas fa-check-circle text-success"></i> Correct
            }
            else
            {
                <i class="fas fa-times-circle text-danger"></i> Incorrect
            }
        </span>
    </div>
    
    <div class="question-text">
        <QuestionRenderer Question="@question" ReadOnly="true" />
    </div>
    
    <div class="your-answer">
        <h5>Your Answer</h5>
        <AnswerDisplay Answer="@question.StudentAnswer" IsCorrect="@question.IsCorrect" />
    </div>
    
    @if (!question.IsCorrect)
    {
        <div class="correct-answer">
            <h5>Correct Answer</h5>
            <AnswerDisplay Answer="@question.CorrectAnswer" IsCorrect="true" />
        </div>
    }
    
    <div class="explanation">
        <h5>Explanation</h5>
        <div class="explanation-text">@((MarkupString)question.CorrectAnswer.Explanation)</div>
    </div>
    
    @if (!string.IsNullOrEmpty(question.AiFeedback))
    {
        <div class="ai-feedback">
            <h5><i class="fas fa-robot"></i> Personalized Feedback</h5>
            <p>@question.AiFeedback</p>
        </div>
    }
    
    @if (question.CommonMistakes?.Any() == true)
    {
        <div class="common-mistakes">
            <h6>Common Mistakes to Avoid:</h6>
            <ul>
                @foreach (var mistake in question.CommonMistakes)
                {
                    <li>@mistake</li>
                }
            </ul>
        </div>
    }
    
    @if (question.LearningResources?.Any() == true)
    {
        <div class="learning-resources">
            <h6>Learn More:</h6>
            <ul>
                @foreach (var resource in question.LearningResources)
                {
                    <li>
                        <a href="@resource.Url" target="_blank">
                            @resource.Title (@resource.Type)
                        </a>
                    </li>
                }
            </ul>
        </div>
    }
</div>
```

**3. Navigation**

- Sticky header with question navigation palette
- Click to jump to specific question
- Color coding: green (correct), red (incorrect), gray (skipped)
- "Previous" / "Next" buttons
- "Return to Results" button

**4. Summary Panel** (sidebar or bottom)

- Questions correct: X / Y
- Total points earned: X / Y
- Percentage: X%
- Subject breakdown: Algebra (80%), Geometry (90%)
- "Retake Assessment" button

## Acceptance Criteria

- [ ] API endpoint returns question review data
- [ ] Endpoint includes AI-generated feedback for each question
- [ ] Review page displays all questions in order
- [ ] Each question shows: question text, student answer, correct answer
- [ ] Correct answers highlighted in green, incorrect in red
- [ ] Explanations provided for all questions
- [ ] AI feedback displayed for incorrect answers
- [ ] Common mistakes section shown when relevant
- [ ] Learning resources links provided
- [ ] Question navigation palette works (jump to question)
- [ ] "Return to Results" button navigates back
- [ ] "Retake Assessment" button starts new attempt
- [ ] Math rendering works (KaTeX)
- [ ] Code syntax highlighting works
- [ ] Page is mobile-responsive
- [ ] Loading state shown while fetching data
- [ ] Error handling for failed API calls
- [ ] Print-friendly CSS for printing review
- [ ] Accessibility: ARIA labels, keyboard navigation
- [ ] Unit tests for component logic (>80% coverage)

## Dependencies

- **Required:**
  - TODO-002: Implement Submit API Endpoint (must have completed assessment)
  - TODO-003: Build Assessment Results Page (navigates from here)
  - KaTeX library (already included)
  - highlight.js (already included)

- **AI Feedback:**
  - Orchestrator should generate feedback during grading
  - Store feedback in database with StudentResponse records

## References

- **Files:**
  - `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentReview.razor` (new)
  - `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentResults.razor.cs` (line 107)
  - `src/AcademicAssessment.Web/Controllers/AssessmentController.cs` (new endpoint)
  
- **Documentation:**
  - `docs/planning/ROADMAP.md` (Week 2-3)
  - `.github/specification/11a-student-workflows.md`

- **Related TODOs:**
  - TODO-002: Implement Submit API Endpoint (prerequisite)
  - TODO-003: Build Assessment Results Page (links to this)

## Implementation Notes

1. **Question Rendering:** Reuse `QuestionRenderer` component with `ReadOnly` mode
2. **AI Feedback:** Should be generated during grading, not on-demand
3. **Explanations:** Support markdown and LaTeX in explanations
4. **Privacy:** Don't show other students' answers (for security)
5. **Caching:** Cache review data on client to avoid refetching
6. **Export:** Consider PDF export functionality
7. **Comparison:** Could show peer performance (anonymized) in future

## UI/UX Considerations

- Use clear visual distinction between correct/incorrect (color + icon)
- Make explanations prominent and easy to read
- Provide encouraging tone even for incorrect answers
- Focus on learning, not just scoring
- Ensure mobile users can read all content comfortably
- Consider collapsible sections for long explanations
- Add "Jump to incorrect answers" quick filter

## Testing Strategy

**Unit Tests:**

- Mock review data from API
- Test component rendering with various question types
- Test navigation between questions
- Verify color coding logic

**Integration Tests:**

- Complete and submit assessment
- Navigate to review page
- Verify all questions displayed correctly
- Test with assessment containing all question types

**Manual Tests:**

- Complete assessment with mixed correct/incorrect
- Review answers and verify accuracy
- Test math rendering in explanations
- Test code highlighting
- Verify on mobile devices
- Test print functionality
- Test accessibility with screen reader
- Verify all navigation works
