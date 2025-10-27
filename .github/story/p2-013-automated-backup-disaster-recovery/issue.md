# Story 013: Automated Backup & Disaster Recovery

**Priority:** P2 - Enhancement  
**Status:** Ready for Implementation  
**Effort:** Medium (1 week)  
**Dependencies:** None


**GitHub Issue:** https://github.com/johnazariah/edumind-ai/issues/18

---

## Problem Statement

No automated backups, no disaster recovery plan. Data loss risk if PostgreSQL server fails.

**Risks:**

- Hardware failure → permanent data loss
- Accidental deletion → no recovery
- Ransomware → encrypted data
- Regional outage → service unavailable

**Business Impact:** Cannot meet RTO (Recovery Time Objective) or RPO (Recovery Point Objective) for enterprise customers.

---

## Goals & Success Criteria

1. **Automated daily backups** to Azure Blob Storage
2. **Point-in-time recovery** (restore to any moment in last 30 days)
3. **Geo-redundant storage** (backup in different Azure region)
4. **Disaster recovery runbook** (step-by-step recovery procedures)
5. **Backup testing** (monthly restore drill)

**Success Criteria:**

- [ ] Daily automated backups running
- [ ] Point-in-time restore tested (RTO <4 hours)
- [ ] Geo-redundant backups in secondary region
- [ ] DR runbook documented and validated
- [ ] RPO <24 hours, RTO <4 hours

---

## Technical Approach

### Backup Strategy

**PostgreSQL Azure Backup:**

- Automated backups every 24 hours
- Retention: 30 days
- Geo-redundant storage (primary: East US, secondary: West US)

**Application State:**

- Redis snapshots (RDB persistence)
- Azure Blob Storage (question images) - already geo-redundant

### Disaster Recovery Tiers

| Component | RPO | RTO | Strategy |
|-----------|-----|-----|----------|
| PostgreSQL | 24h | 4h | Automated backups |
| Redis | 1h | 30min | RDB snapshots |
| Blob Storage | <1min | <1min | Geo-redundant |
| Application | N/A | 30min | Redeploy containers |

---

## Task Decomposition

### Task 1: Enable Azure PostgreSQL Automated Backup

- **Location:** Azure Portal → PostgreSQL → Backup
- **Config:**
  - Backup retention: 30 days
  - Geo-redundant: Enabled
  - Point-in-time restore: Enabled
- **Acceptance:** Daily backups running

### Task 2: Configure Redis Persistence

- **Files to Modify:** `docker-compose.yml` (local), Azure Redis config (prod)
- **RDB Config:**

  ```
  save 900 1      # Save if 1 key changed in 15 min
  save 300 10     # Save if 10 keys changed in 5 min
  save 60 10000   # Save if 10k keys changed in 1 min
  ```

- **Acceptance:** Redis snapshots written to disk

### Task 3: Create Backup Verification Script

- **Files to Create:**
  - `scripts/verify-backups.sh`
- **Check:**
  - PostgreSQL backup timestamp <24h
  - Redis RDB file exists and <1h old
  - Blob storage replication status
- **Schedule:** Daily via Azure Logic App
- **Acceptance:** Script detects missing backups

### Task 4: Create Disaster Recovery Runbook

- **Files to Create:**
  - `docs/operations/DISASTER_RECOVERY_RUNBOOK.md`
- **Procedures:**
  - How to restore PostgreSQL from backup
  - How to restore Redis snapshot
  - How to fail over to secondary region
  - Contact escalation tree
- **Acceptance:** Runbook complete and reviewed

### Task 5: Perform DR Drill

- **Schedule:** Monthly
- **Steps:**
  1. Restore PostgreSQL backup to test server
  2. Verify data integrity (row counts, sample queries)
  3. Restore Redis snapshot
  4. Deploy application pointing to restored DB
  5. Run smoke tests
  6. Document time taken (RTO)
- **Acceptance:** Successful restore in <4 hours

### Task 6: Set Up Cross-Region Replication

- **PostgreSQL:** Geo-redundant backup (already enabled in Task 1)
- **Blob Storage:** GRS (Geo-Redundant Storage) - already default
- **Application:** Deploy standby in secondary region (manual failover)
- **Acceptance:** All data replicated to secondary region

---

## Acceptance Criteria

- [ ] Daily automated backups configured
- [ ] Point-in-time restore tested successfully
- [ ] Geo-redundant backups in secondary region
- [ ] DR runbook documented
- [ ] Monthly DR drill scheduled
- [ ] RPO <24h, RTO <4h validated

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot
