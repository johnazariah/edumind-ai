# EduMind.AI Deployment Playbooks

Scenario-driven operational guides for deploying and managing EduMind.AI.

---

## 📚 Quick Navigation

### Getting Started

- **[01 - First-Time Local Setup](./01-first-time-local-setup.md)** ⭐  
  Set up development environment from scratch (30-45 min)

- **[02 - Daily Development Workflow](./02-daily-development-workflow.md)** ⭐  
  Typical day-to-day operations (5-10 min per session)

### Azure Deployment

- **[03 - First-Time Azure Deployment](./03-first-time-azure-deployment.md)** ⭐  
  Deploy to Azure Container Apps for the first time (60-90 min)

### Operations

- **[04 - Troubleshoot Unhealthy Services](./04-troubleshoot-unhealthy-services.md)**  
  Diagnose and fix health check failures (15-30 min)

- **[05 - Rollback Deployment](./05-rollback-deployment.md)**  
  Revert to previous version when things go wrong (5-15 min)

- **[06 - View Logs and Monitor](./06-view-logs-and-monitor.md)**  
  Access logs, monitor performance, set up alerts (5-15 min)

---

## 🎯 Choose Your Playbook

### I'm a New Developer

1. Start with **[01 - First-Time Local Setup](./01-first-time-local-setup.md)**
2. Once working, read **[02 - Daily Development Workflow](./02-daily-development-workflow.md)**
3. Keep **[04 - Troubleshoot Unhealthy Services](./04-troubleshoot-unhealthy-services.md)** handy

### I Need to Deploy to Azure

1. Complete **[01 - First-Time Local Setup](./01-first-time-local-setup.md)** first (verify local works)
2. Follow **[03 - First-Time Azure Deployment](./03-first-time-azure-deployment.md)**
3. Bookmark **[04 - Troubleshoot Unhealthy Services](./04-troubleshoot-unhealthy-services.md)** for issues

### Something is Broken

1. Check **[04 - Troubleshoot Unhealthy Services](./04-troubleshoot-unhealthy-services.md)** first
2. Review **[06 - View Logs and Monitor](./06-view-logs-and-monitor.md)** to find error details
3. If recent deployment caused issue, use **[05 - Rollback Deployment](./05-rollback-deployment.md)**

### I Need to Investigate an Issue

1. Start with **[06 - View Logs and Monitor](./06-view-logs-and-monitor.md)**
2. Use logs to identify root cause
3. Apply fix or use **[05 - Rollback Deployment](./05-rollback-deployment.md)** if critical

---

## 📖 Playbook Structure

Each playbook follows this format:

- **Scenario:** What situation this playbook addresses
- **Time Required:** Estimated completion time
- **Difficulty:** Beginner / Intermediate / Advanced
- **Prerequisites:** What you need before starting
- **Steps:** Detailed, copy-paste-friendly instructions
- **Verification:** How to confirm success
- **Troubleshooting:** Common issues and solutions
- **Next Steps:** What to do after completing this playbook

---

## 🔧 Related Documentation

### Technical Reference

For detailed technical specifications, command references, and architecture:

- **[Deployment Reference](../reference.md)** - Comprehensive technical documentation

### Architecture & Planning

For understanding system design and decisions:

- **[ADR Directory](../../adr/)** - Architectural Decision Records
- **[System Architecture](../../../docs/architecture/ARCHITECTURE_SUMMARY.md)** - High-level overview
- **[Solution Structure](../../../docs/architecture/SOLUTION_STRUCTURE.md)** - Project organization

### Source Documentation

Original deployment documentation (now consolidated into playbooks):

- **[docs/deployment/](../../../docs/deployment/)** - Historical deployment docs
- **[.github/workflows/](../../workflows/)** - CI/CD pipeline definitions

---

## 💡 Tips for Using Playbooks

### Copy-Paste Commands

All commands in playbooks are designed to be copy-paste friendly. Replace placeholders:

- `<your-subscription-id>` - Your Azure subscription ID
- `<container-id>` - Docker container ID
- `<app-insights-name>` - Your Application Insights resource name
- `dev` - Environment name (dev, staging, prod)

### Command Explanations

Commands include inline comments explaining what they do:

```bash
# This comment explains what the command does
az containerapp show --name ca-webapi-dev --resource-group rg-dev

# Multi-line commands use backslashes
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --tail 100
```

### Verification Steps

Each major step includes a verification command to confirm success. Look for:

- **Expected output:** What you should see
- **If not:** Troubleshooting steps

### Time Estimates

- **Beginner:** No prior experience with the technology
- **Intermediate:** Familiar with Azure/Docker/CLI basics
- **Advanced:** Deep expertise, comfortable with KQL and infrastructure

Time estimates assume:

- Stable internet connection
- All prerequisites met
- No unexpected errors

Add 50% buffer time if you're learning.

---

## 🚨 Emergency Procedures

### Production is Down

1. **Immediate:** Use **[05 - Rollback Deployment](./05-rollback-deployment.md)** Emergency Rollback (2 min)
2. **Verify:** Check health endpoints (1 min)
3. **Monitor:** Use **[06 - View Logs and Monitor](./06-view-logs-and-monitor.md)** to watch for issues
4. **Post-Mortem:** Document incident, schedule fix

### Health Checks Failing

1. **Diagnose:** Use **[04 - Troubleshoot Unhealthy Services](./04-troubleshoot-unhealthy-services.md)** Quick Diagnosis
2. **Identify Component:** PostgreSQL, Redis, or Agents?
3. **Fix or Rollback:** Apply fix or use **[05 - Rollback Deployment](./05-rollback-deployment.md)**

### Can't Access Logs

1. Check Azure CLI authentication: `az account show`
2. Verify resource group exists: `az group show --name rg-dev`
3. Use Azure Portal as backup: <https://portal.azure.com>
4. Check Application Insights if Container Apps logs unavailable

---

## 🔄 Playbook Maintenance

These playbooks are living documents. Update them when:

- New deployment steps are added
- Known issues are resolved
- Better workarounds are found
- Tools are updated (azd, Azure CLI, etc.)

**Last Updated:** 2025-10-24  
**Version:** 1.0.0

---

## 📞 Getting Help

If playbooks don't solve your issue:

1. **Check GitHub Issues:** <https://github.com/johnazariah/edumind-ai/issues>
2. **Review Reference Guide:** [../reference.md](../reference.md)
3. **Check ADRs:** [../../adr/](../../adr/) for architectural context
4. **Contact Team:** Create GitHub issue with:
   - Which playbook you were following
   - What step failed
   - Error messages
   - Environment (local/Azure, OS, versions)

---

## 🎓 Learning Path

### Week 1: Local Development

- Day 1: Complete **01 - First-Time Local Setup**
- Day 2-5: Practice **02 - Daily Development Workflow**
- End of Week: Run through **04 - Troubleshoot Unhealthy Services** scenarios

### Week 2: Azure Deployment

- Day 1: Complete **03 - First-Time Azure Deployment** to staging
- Day 2-3: Practice **06 - View Logs and Monitor** on staging
- Day 4: Test **05 - Rollback Deployment** on staging
- Day 5: Deploy to production with team supervision

### Ongoing

- Review playbooks monthly for updates
- Practice rollback procedures quarterly
- Update playbooks when you discover better approaches

---

**Ready to start?** Begin with **[01 - First-Time Local Setup](./01-first-time-local-setup.md)** ⭐
