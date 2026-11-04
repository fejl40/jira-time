---
id: TASK-121.3
title: Health check section on Settings
status: Done
assignee: []
created_date: '2026-11-04 09:06'
updated_date: '2026-11-04 09:18'
labels:
  - jira
  - settings
dependencies: []
parent_task_id: TASK-121
priority: medium
ordinal: 213000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The Settings section: Run button, one row per check with tick, cross or skipped, and the fix sentence; data-health-check attributes for a browser check.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Rows update as each check finishes
- [x] #2 Checked in the browser against the mock Jira
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Components/Pages/HealthCheckSection.razor (and HealthCheckIcon.razor) on Settings: Run button with the Waiting spinner, one row per check (data-health-check, data-health-state, data-health-detail) updating as each check starts and finishes, a progress bar, a summary (data-health-summary) with a Logs link when something failed. Checked with Playwright against the mock Jira: light, dark, phone, reduced motion, all passing and with failures.
<!-- SECTION:FINAL_SUMMARY:END -->
