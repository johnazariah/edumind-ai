# TODO-020: Complete Accessibility Features (WCAG 2.1 AA)

**Priority:** P2 - Medium  
**Area:** Frontend / UX  
**Estimated Effort:** Large (8-12 hours)  
**Status:** Not Started

## Description

Implement comprehensive accessibility features across all student-facing UI to achieve WCAG 2.1 Level AA compliance, ensuring the platform is usable by students with disabilities.

## Context

Accessibility is a legal requirement for educational software (Section 508, ADA) and a moral imperative. Currently, the Student App has basic Bootstrap accessibility, but many components need:

- ARIA labels and roles
- Keyboard navigation support
- Screen reader compatibility
- Focus management
- Color contrast compliance
- Text scaling support
- Alternative text for all images
- Semantic HTML

This work is planned for **Week 3, Days 15-16** in the roadmap (Task 2.11).

## Technical Requirements

### ARIA Labels and Roles

**Assessment Cards:**

```razor
<div class="card" role="article" aria-labelledby="assessment-title-@assessment.Id">
    <h5 id="assessment-title-@assessment.Id">@assessment.Title</h5>
    <button aria-label="Start @assessment.Title assessment" class="btn btn-primary">
        Start Assessment
    </button>
</div>
```

**Question Renderer:**

```razor
<div role="group" aria-labelledby="question-text">
    <p id="question-text">@question.Text</p>
    <div role="radiogroup" aria-labelledby="question-text">
        @foreach (var option in question.Options)
        {
            <input type="radio" 
                   id="option-@option.Id" 
                   name="answer" 
                   aria-label="Option: @option.Text" />
            <label for="option-@option.Id">@option.Text</label>
        }
    </div>
</div>
```

**Progress Indicators:**

```razor
<div role="progressbar" 
     aria-valuenow="@currentQuestion" 
     aria-valuemin="0" 
     aria-valuemax="@totalQuestions"
     aria-label="Assessment progress: @currentQuestion of @totalQuestions questions">
    <div class="progress-bar" style="width: @progressPercentage%"></div>
</div>
```

### Keyboard Navigation

**Required Shortcuts:**

- `Tab` / `Shift+Tab` - Navigate between interactive elements
- `Enter` / `Space` - Activate buttons and links
- `Arrow keys` - Navigate between questions or options
- `Ctrl+S` - Save progress
- `Ctrl+Enter` - Submit assessment (with confirmation)
- `Escape` - Close modals or cancel actions
- `?` - Show keyboard shortcuts help

**Implementation:**

```csharp
@code {
    private void HandleKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowRight":
            case "ArrowDown":
                if (!isLastQuestion) NextQuestion();
                break;
            case "ArrowLeft":
            case "ArrowUp":
                if (!isFirstQuestion) PreviousQuestion();
                break;
            case "s" when e.CtrlKey:
                e.PreventDefault();
                await SaveProgressAsync();
                break;
            case "Enter" when e.CtrlKey:
                e.PreventDefault();
                await ConfirmSubmitAsync();
                break;
        }
    }
}
```

### Screen Reader Support

**Live Regions for Dynamic Updates:**

```razor
<div role="status" aria-live="polite" aria-atomic="true" class="sr-only">
    @statusMessage
</div>

<!-- Example: "Answer saved successfully" -->
<!-- Example: "Question 5 of 25" -->
<!-- Example: "Warning: 5 minutes remaining" -->
```

**Skip Links:**

```razor
<a href="#main-content" class="skip-link">Skip to main content</a>
<a href="#navigation" class="skip-link">Skip to navigation</a>

<main id="main-content" tabindex="-1">
    <!-- Content -->
</main>
```

### Color Contrast

**WCAG AA Requirements:**

- Normal text (< 18pt): Contrast ratio ≥ 4.5:1
- Large text (≥ 18pt): Contrast ratio ≥ 3:1
- UI components: Contrast ratio ≥ 3:1

**Audit and Fix:**

- Check all text colors against backgrounds
- Ensure buttons have sufficient contrast
- Verify focus indicators are visible (2px solid, high contrast)
- Test in high contrast mode (Windows/macOS)
- Don't rely on color alone (use icons, text, patterns)

**Example:**

```css
/* Bad: Low contrast */
.btn-primary {
    background: #ccc;
    color: #aaa;
}

/* Good: High contrast */
.btn-primary {
    background: #0056b3;
    color: #ffffff; /* 4.5:1 contrast ratio */
}

/* Visible focus indicator */
*:focus {
    outline: 2px solid #0056b3;
    outline-offset: 2px;
}
```

### Focus Management

**Modal Dialogs:**

```csharp
private ElementReference firstFocusableElement;
private ElementReference lastFocusableElement;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (isModalOpen)
    {
        await firstFocusableElement.FocusAsync();
    }
}

// Trap focus within modal
private void HandleModalKeyDown(KeyboardEventArgs e)
{
    if (e.Key == "Tab")
    {
        if (e.ShiftKey && focusedElement == firstFocusableElement)
        {
            e.PreventDefault();
            await lastFocusableElement.FocusAsync();
        }
        else if (!e.ShiftKey && focusedElement == lastFocusableElement)
        {
            e.PreventDefault();
            await firstFocusableElement.FocusAsync();
        }
    }
}
```

### Text Scaling

- Support browser zoom up to 200%
- Use relative units (rem, em) instead of px
- Ensure layout doesn't break at 200% zoom
- Test with browser text-only zoom
- Ensure all text is readable

```css
/* Use rem for scalability */
body {
    font-size: 16px; /* Base size */
}

h1 { font-size: 2rem; } /* 32px */
p { font-size: 1rem; } /* 16px */
small { font-size: 0.875rem; } /* 14px */
```

### Alternative Text

**Images:**

```razor
<img src="diagram.png" alt="Diagram showing the Pythagorean theorem: a² + b² = c²" />
```

**Icons:**

```razor
<i class="fas fa-check" aria-label="Correct answer" role="img"></i>
<i class="fas fa-times" aria-hidden="true"></i> <!-- Decorative icon -->
<span class="sr-only">Incorrect answer</span>
```

**Charts:**

```razor
<canvas id="performanceChart" role="img" 
        aria-label="Bar chart showing performance across 5 subjects. 
                    Math: 85%, Science: 90%, English: 75%, History: 80%, Art: 95%">
</canvas>
<!-- Provide text alternative or table below chart -->
```

## Acceptance Criteria

- [ ] All interactive elements have accessible names (aria-label or visible text)
- [ ] All images and icons have appropriate alt text or aria-labels
- [ ] Keyboard navigation works for all interactive elements
- [ ] Focus indicators are visible (2px solid, high contrast)
- [ ] Skip links provided for main content and navigation
- [ ] All forms have associated labels
- [ ] Error messages are programmatically associated with inputs
- [ ] Live regions announce dynamic content changes
- [ ] Modal dialogs trap focus and return focus on close
- [ ] Color contrast ratio ≥ 4.5:1 for normal text
- [ ] Color contrast ratio ≥ 3:1 for large text and UI components
- [ ] Layout works at 200% zoom
- [ ] All text uses relative units (rem, em)
- [ ] Semantic HTML used throughout (nav, main, article, section)
- [ ] ARIA roles used appropriately (don't override semantic HTML)
- [ ] Screen reader testing completed (NVDA, JAWS, VoiceOver)
- [ ] Keyboard-only testing completed
- [ ] Automated accessibility testing passes (axe, WAVE)
- [ ] Manual testing with assistive technologies
- [ ] Accessibility statement page created
- [ ] User documentation includes accessibility features

## Dependencies

- **Required:**
  - Bootstrap's built-in accessibility features
  - Browser support for ARIA
  - Screen reader testing software

- **Tools:**
  - axe DevTools (browser extension)
  - WAVE Web Accessibility Evaluation Tool
  - Lighthouse accessibility audit
  - NVDA (Windows screen reader - free)
  - VoiceOver (macOS/iOS - built-in)

## References

- **Standards:**
  - WCAG 2.1 Level AA: <https://www.w3.org/WAI/WCAG21/quickref/?versions=2.1>
  - Section 508: <https://www.section508.gov/>
  - ARIA Authoring Practices: <https://www.w3.org/WAI/ARIA/apg/>
  
- **Files:**
  - All components in `src/AcademicAssessment.StudentApp/Components/`
  - `src/AcademicAssessment.StudentApp/wwwroot/css/site.css`
  
- **Documentation:**
  - `docs/planning/ROADMAP.md` (Week 3, Task 2.11)
  - `.github/specification/11a-student-workflows.md`

- **Related TODOs:**
  - TODO-021: Mobile-Responsive Design (Week 3, Task 2.12)
  - TODO-025: UX Refinements (Week 3, Task 2.15)

## Implementation Notes

1. **Incremental Approach:** Fix one component at a time
2. **Automated Testing:** Run axe in CI/CD pipeline
3. **Screen Reader Testing:** Test with at least 2 different screen readers
4. **Real Users:** If possible, test with users who rely on assistive tech
5. **Documentation:** Create accessibility statement page
6. **Training:** Document accessibility patterns for future development
7. **Maintenance:** Set up periodic accessibility audits

## Testing Strategy

**Automated Testing:**

```bash
# Run axe accessibility tests
npm install @axe-core/cli -g
axe https://localhost:5049 --tags wcag2a,wcag2aa
```

**Manual Testing Checklist:**

- [ ] Navigate entire app using only keyboard
- [ ] Test with NVDA screen reader (Windows)
- [ ] Test with VoiceOver screen reader (macOS)
- [ ] Zoom to 200% and verify layout
- [ ] Enable high contrast mode and verify visibility
- [ ] Test with browser text-only zoom
- [ ] Disable CSS and verify content order
- [ ] Test all forms with screen reader
- [ ] Test all modals with keyboard navigation
- [ ] Verify all error messages are announced
- [ ] Test live regions announce updates

**Component-Specific Tests:**

- Assessment cards: keyboard navigation, screen reader
- Question renderer: ARIA roles, keyboard input
- Progress bars: aria-valuenow updates
- Modals: focus trap, Escape key
- Timers: live region updates
- Buttons: accessible names, focus indicators
