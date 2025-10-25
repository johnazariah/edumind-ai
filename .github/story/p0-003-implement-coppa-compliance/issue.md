# Story 003: Implement COPPA Compliance (Age Verification & Parental Consent)

**Priority:** P0 - Critical Blocker  
**Status:** Ready for Implementation  
**Effort:** Large (1-2 weeks)  
**Dependencies:** None

---

## Problem Statement

EduMind.AI currently lacks COPPA (Children's Online Privacy Protection Act) compliance mechanisms, which is a critical legal requirement for serving students under 13 years old in the United States.

**Legal Requirements:**

- Age verification on signup
- Parental consent for users under 13
- Restricted data collection for minors
- Parental access to child's data

**Current Status:** Authentication exists (Azure AD B2C), but no age verification or parental consent workflows.

---

## Goals & Success Criteria

### Goals

- Achieve full COPPA compliance for U.S. market
- Implement age-gated signup flow
- Build parental consent request and approval workflow
- Restrict data collection for users under 13
- Enable parents to view/delete child's data

### Success Criteria

- [ ] Age verification on user registration
- [ ] Parental consent email workflow
- [ ] Under-13 users blocked until consent received
- [ ] Parent portal for child data management
- [ ] Audit trail of consent decisions
- [ ] Compliance with COPPA safe harbor guidelines

---

## Technical Approach

### Architecture Overview

```
Student Signup Flow:
1. Student enters age/birthdate
2. If age < 13 → Request parental consent
3. System sends email to parent with consent link
4. Parent reviews privacy policy and approves
5. Student account activated

Parent Portal:
- View child's account information
- Review assessment history
- Request data deletion
- Revoke consent (deactivates account)
```

### Database Schema

**New Tables:**

```sql
CREATE TABLE parental_consents (
    id UUID PRIMARY KEY,
    student_id UUID NOT NULL REFERENCES students(id),
    parent_email VARCHAR(255) NOT NULL,
    parent_name VARCHAR(255),
    consent_token UUID NOT NULL UNIQUE,
    consent_requested_at TIMESTAMP NOT NULL,
    consent_granted_at TIMESTAMP,
    consent_status VARCHAR(20) NOT NULL, -- 'Pending', 'Granted', 'Denied', 'Revoked'
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE TABLE age_verifications (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    date_of_birth DATE NOT NULL,
    age_at_signup INT NOT NULL,
    verification_method VARCHAR(50), -- 'SelfReported', 'IDVerified'
    requires_parental_consent BOOLEAN NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Service Components

1. **AgeVerificationService** - Calculate age and determine COPPA applicability
2. **ParentalConsentService** - Manage consent workflow
3. **EmailService** - Send consent request emails
4. **ParentPortalService** - Parent data access and management

---

## Task Decomposition

### Task 1: Create Domain Models for Age Verification

- **Description:** Define domain entities for age verification and parental consent
- **Files:**
  - `src/AcademicAssessment.Core/Entities/AgeVerification.cs` (new)
  - `src/AcademicAssessment.Core/Entities/ParentalConsent.cs` (new)
  - `src/AcademicAssessment.Core/Enums/ConsentStatus.cs` (new)
- **Code:**

  ```csharp
  public record AgeVerification
  {
      public Guid Id { get; init; }
      public Guid UserId { get; init; }
      public DateOnly DateOfBirth { get; init; }
      public int AgeAtSignup { get; init; }
      public string VerificationMethod { get; init; } = "SelfReported";
      public bool RequiresParentalConsent { get; init; }
      public DateTime CreatedAt { get; init; }
  }
  
  public record ParentalConsent
  {
      public Guid Id { get; init; }
      public Guid StudentId { get; init; }
      public string ParentEmail { get; init; } = string.Empty;
      public string? ParentName { get; init; }
      public Guid ConsentToken { get; init; }
      public DateTime ConsentRequestedAt { get; init; }
      public DateTime? ConsentGrantedAt { get; init; }
      public ConsentStatus Status { get; init; }
      public string? IpAddress { get; init; }
      public string? UserAgent { get; init; }
  }
  
  public enum ConsentStatus
  {
      Pending,
      Granted,
      Denied,
      Revoked
  }
  ```

- **Acceptance:** Models compiled, follow coding standards
- **Dependencies:** None

### Task 2: Create Database Migration for COPPA Tables

- **Description:** Add database tables for age verification and parental consent
- **Files:**
  - `src/AcademicAssessment.Infrastructure/Data/Migrations/AddCOPPATables.cs` (new)
- **SQL:**

  ```sql
  CREATE TABLE age_verifications (
      id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
      date_of_birth DATE NOT NULL,
      age_at_signup INT NOT NULL,
      verification_method VARCHAR(50) NOT NULL DEFAULT 'SelfReported',
      requires_parental_consent BOOLEAN NOT NULL,
      created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
  );
  
  CREATE TABLE parental_consents (
      id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
      parent_email VARCHAR(255) NOT NULL,
      parent_name VARCHAR(255),
      consent_token UUID NOT NULL UNIQUE DEFAULT gen_random_uuid(),
      consent_requested_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
      consent_granted_at TIMESTAMP,
      consent_status VARCHAR(20) NOT NULL DEFAULT 'Pending',
      ip_address INET,
      user_agent TEXT,
      created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
      updated_at TIMESTAMP,
      CONSTRAINT chk_consent_status CHECK (consent_status IN ('Pending', 'Granted', 'Denied', 'Revoked'))
  );
  
  CREATE INDEX idx_parental_consents_student ON parental_consents(student_id);
  CREATE INDEX idx_parental_consents_token ON parental_consents(consent_token);
  CREATE INDEX idx_age_verifications_user ON age_verifications(user_id);
  ```

- **Acceptance:** Migration applies successfully, tables created
- **Dependencies:** Task 1

### Task 3: Implement AgeVerificationService

- **Description:** Service to calculate age and determine COPPA requirements
- **Files:**
  - `src/AcademicAssessment.Core/Services/IAgeVerificationService.cs` (new)
  - `src/AcademicAssessment.Core/Services/AgeVerificationService.cs` (new)
- **Code:**

  ```csharp
  public interface IAgeVerificationService
  {
      Task<Result<AgeVerification>> VerifyAgeAsync(Guid userId, DateOnly dateOfBirth, CancellationToken cancellationToken);
      int CalculateAge(DateOnly dateOfBirth, DateTime? referenceDate = null);
      bool RequiresParentalConsent(int age);
  }
  
  public class AgeVerificationService : IAgeVerificationService
  {
      private const int CoppaAgeThreshold = 13;
      private readonly IAgeVerificationRepository _repository;
      
      public int CalculateAge(DateOnly dateOfBirth, DateTime? referenceDate = null)
      {
          var reference = referenceDate ?? DateTime.UtcNow;
          var today = DateOnly.FromDateTime(reference);
          var age = today.Year - dateOfBirth.Year;
          
          // Adjust if birthday hasn't occurred this year
          if (dateOfBirth > today.AddYears(-age))
              age--;
          
          return age;
      }
      
      public bool RequiresParentalConsent(int age) => age < CoppaAgeThreshold;
      
      public async Task<Result<AgeVerification>> VerifyAgeAsync(
          Guid userId, 
          DateOnly dateOfBirth, 
          CancellationToken cancellationToken)
      {
          var age = CalculateAge(dateOfBirth);
          var verification = new AgeVerification
          {
              Id = Guid.NewGuid(),
              UserId = userId,
              DateOfBirth = dateOfBirth,
              AgeAtSignup = age,
              VerificationMethod = "SelfReported",
              RequiresParentalConsent = RequiresParentalConsent(age),
              CreatedAt = DateTime.UtcNow
          };
          
          await _repository.AddAsync(verification, cancellationToken);
          return Result<AgeVerification>.Success(verification);
      }
  }
  ```

- **Acceptance:** Service compiles, unit tests pass
- **Dependencies:** Task 1

### Task 4: Implement ParentalConsentService

- **Description:** Service to manage parental consent workflow
- **Files:**
  - `src/AcademicAssessment.Core/Services/IParentalConsentService.cs` (new)
  - `src/AcademicAssessment.Core/Services/ParentalConsentService.cs` (new)
- **Methods:**
  - `RequestConsentAsync(studentId, parentEmail, parentName)` - Create consent request
  - `GrantConsentAsync(consentToken, ipAddress, userAgent)` - Parent grants consent
  - `DenyConsentAsync(consentToken)` - Parent denies consent
  - `RevokeConsentAsync(studentId)` - Parent revokes consent
  - `GetConsentStatusAsync(studentId)` - Check current status
- **Acceptance:** Service implements consent lifecycle
- **Dependencies:** Task 1

### Task 5: Implement Email Service for Consent Requests

- **Description:** Send consent request emails to parents
- **Files:**
  - `src/AcademicAssessment.Infrastructure/Email/IEmailService.cs` (new)
  - `src/AcademicAssessment.Infrastructure/Email/EmailService.cs` (new)
  - `src/AcademicAssessment.Infrastructure/Email/Templates/ParentalConsentRequest.cshtml` (new)
- **Email Template:**

  ```html
  <h2>Parental Consent Required for EduMind.AI</h2>
  <p>Hello @Model.ParentName,</p>
  <p>Your child has registered for EduMind.AI. Because they are under 13 years old, 
     we require your consent before they can use the platform.</p>
  
  <p><strong>Please review our privacy policy and grant consent:</strong></p>
  <a href="@Model.ConsentUrl" class="btn">Review and Grant Consent</a>
  
  <p>This consent link expires in 7 days.</p>
  ```

- **Configuration:** Use Azure Communication Services or SendGrid
- **Acceptance:** Emails sent successfully, links work
- **Dependencies:** Task 4

### Task 6: Create Parental Consent API Endpoints

- **Description:** API endpoints for consent workflow
- **Files:**
  - `src/AcademicAssessment.Web/Controllers/ParentalConsentController.cs` (new)
- **Endpoints:**
  - `POST /api/consent/request` - Request consent
  - `GET /api/consent/{token}` - View consent request (public)
  - `POST /api/consent/{token}/grant` - Grant consent (public)
  - `POST /api/consent/{token}/deny` - Deny consent (public)
  - `GET /api/consent/status/{studentId}` - Check status (authenticated)
  - `POST /api/consent/{studentId}/revoke` - Revoke consent (parent authenticated)
- **Acceptance:** All endpoints functional, properly secured
- **Dependencies:** Task 4

### Task 7: Update Registration Flow with Age Verification

- **Description:** Add date of birth field to student registration
- **Files:**
  - `src/AcademicAssessment.StudentApp/Pages/Register.razor`
  - `src/AcademicAssessment.StudentApp/Services/RegistrationService.cs`
- **Flow:**
  1. Student enters email, password, name, **date of birth**
  2. System calculates age
  3. If age < 13:
     - Show "Parental Consent Required" message
     - Request parent email
     - Send consent email
     - Account created but inactive
  4. If age >= 13:
     - Standard registration flow
     - Account activated immediately
- **Acceptance:** Registration flow includes age gate
- **Dependencies:** Task 3, Task 5

### Task 8: Build Parent Consent Portal UI

- **Description:** Web page for parents to review and grant consent
- **Files:**
  - `src/AcademicAssessment.Web/Pages/ParentalConsent.cshtml` (new Razor Page)
  - `src/AcademicAssessment.Web/Pages/ParentalConsent.cshtml.cs` (new)
- **UI Components:**
  - Privacy policy display
  - Data collection disclosure
  - Child's information review
  - "I Consent" button
  - "I Do Not Consent" button
  - Legal disclaimers
- **Acceptance:** Parents can grant/deny consent via web page
- **Dependencies:** Task 6

### Task 9: Implement Account Activation After Consent

- **Description:** Activate student accounts when consent granted
- **Files:**
  - `src/AcademicAssessment.Core/Services/UserActivationService.cs` (new)
- **Logic:**

  ```csharp
  public async Task<Result> ActivateAccountAsync(Guid studentId, CancellationToken cancellationToken)
  {
      // Check consent status
      var consent = await _consentService.GetConsentStatusAsync(studentId, cancellationToken);
      
      if (consent.Status != ConsentStatus.Granted)
          return Result.Failure(new Error("Account.ConsentRequired", "Parental consent not granted"));
      
      // Activate user account
      var user = await _userRepository.GetByIdAsync(studentId, cancellationToken);
      user = user with { IsActive = true, ActivatedAt = DateTime.UtcNow };
      await _userRepository.UpdateAsync(user, cancellationToken);
      
      return Result.Success();
  }
  ```

- **Acceptance:** Accounts activated when consent granted
- **Dependencies:** Task 4

### Task 10: Build Parent Dashboard for Child Data Management

- **Description:** Portal for parents to view and manage child's data
- **Files:**
  - `src/AcademicAssessment.Dashboard/Pages/ParentPortal.razor` (new)
- **Features:**
  - View child's profile
  - View assessment history (anonymized)
  - Request data export (JSON format)
  - Request data deletion
  - Revoke consent (deactivate account)
- **Authentication:** Parent email verification or Azure AD B2C
- **Acceptance:** Parents can manage child's data
- **Dependencies:** Task 4, Task 6

### Task 11: Implement Audit Logging for Consent Actions

- **Description:** Log all consent-related actions for compliance
- **Files:**
  - `src/AcademicAssessment.Infrastructure/Audit/ConsentAuditLogger.cs` (new)
- **Logged Events:**
  - Consent requested
  - Consent granted (with IP, user agent, timestamp)
  - Consent denied
  - Consent revoked
  - Parent portal access
  - Data deletion requests
- **Acceptance:** All consent actions logged to audit trail
- **Dependencies:** Task 4

### Task 12: Write Unit Tests for Age Verification

- **Description:** Comprehensive tests for age calculation and COPPA logic
- **Files:**
  - `tests/AcademicAssessment.Tests.Unit/Services/AgeVerificationServiceTests.cs` (new)
- **Test Cases:**
  - Age 12 → RequiresParentalConsent = true
  - Age 13 → RequiresParentalConsent = false
  - Birthday edge cases (today, yesterday, tomorrow)
  - Leap year birthdays
- **Acceptance:** 100% coverage for age verification logic
- **Dependencies:** Task 3

### Task 13: Write Integration Tests for Consent Workflow

- **Description:** End-to-end tests for parental consent flow
- **Files:**
  - `tests/AcademicAssessment.Tests.Integration/COPPA/ParentalConsentWorkflowTests.cs` (new)
- **Test Scenarios:**
  1. Under-13 registration → Consent requested
  2. Parent grants consent → Account activated
  3. Parent denies consent → Account remains inactive
  4. Consent expiration → Account deactivated after 7 days
  5. Parent revokes consent → Account deactivated
- **Acceptance:** All consent workflows tested
- **Dependencies:** Task 6, Task 9

### Task 14: Update Documentation for COPPA Compliance

- **Description:** Document COPPA implementation and compliance status
- **Files:**
  - `.github/specification/07-security-privacy.md`
  - `docs/compliance/COPPA_COMPLIANCE.md` (new)
  - `README.md`
- **Content:**

  ```markdown
  ## COPPA Compliance
  
  **Status:** ✅ Implemented (2025-10-25)
  
  EduMind.AI is fully compliant with the Children's Online Privacy Protection Act (COPPA).
  
  ### Features
  - Age verification on registration
  - Parental consent workflow for users under 13
  - Email-based consent requests
  - Parent portal for data management
  - Audit trail of all consent actions
  - Data deletion upon request
  
  ### Implementation
  - Age threshold: 13 years
  - Consent expiration: 7 days
  - Parent verification: Email-based
  - Audit retention: 7 years
  ```

- **Acceptance:** Compliance status documented
- **Dependencies:** Task 13

---

## Acceptance Criteria (Validation)

### Functional Testing

1. **Under-13 Registration:**
   - Register with DOB showing age 12
   - Expected: "Parental Consent Required" message
   - Expected: Consent email sent to parent
   - Expected: Account inactive

2. **Parent Consent Grant:**
   - Click consent link in email
   - Review privacy policy
   - Click "I Consent"
   - Expected: Student account activated
   - Expected: Student can log in

3. **Parent Consent Denial:**
   - Click consent link
   - Click "I Do Not Consent"
   - Expected: Account remains inactive
   - Expected: Student cannot log in

4. **Over-13 Registration:**
   - Register with DOB showing age 14
   - Expected: Standard registration flow
   - Expected: Account activated immediately
   - Expected: No parental consent required

### Compliance Verification

- [ ] Age calculation accurate
- [ ] Consent requests sent within 24 hours
- [ ] Parent identity verified (email confirmation)
- [ ] Consent decisions logged with timestamp, IP, user agent
- [ ] Data deletion honored within 30 days
- [ ] Audit trail retained for 7 years

### Security Testing

- [ ] Consent tokens are cryptographically secure
- [ ] Consent links expire after 7 days
- [ ] Parent portal requires authentication
- [ ] SQL injection prevented on all inputs
- [ ] XSS prevented in consent forms

---

## Context & References

### Documentation

- [Security & Privacy Specification](.github/specification/07-security-privacy.md)
- [User Registration Workflow](.github/specification/11a-student-workflows.md)

### Compliance References

- [COPPA Rule](https://www.ftc.gov/enforcement/rules/rulemaking-regulatory-reform-proceedings/childrens-online-privacy-protection-rule)
- [FTC COPPA FAQ](https://www.ftc.gov/business-guidance/resources/complying-coppa-frequently-asked-questions)

---

## Notes

- **Safe Harbor:** Consider FTC-approved safe harbor program for additional protection
- **Age Verification:** Self-reported DOB is acceptable; ID verification optional enhancement
- **Email Delivery:** Monitor consent email deliverability (spam filters)
- **International:** COPPA is U.S.-specific; GDPR requires consent for all minors under 16

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot  
**Last Updated:** 2025-10-25
