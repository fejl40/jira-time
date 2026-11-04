---
id: TASK-121.1
title: Health check rules
status: Done
assignee: []
created_date: '2026-11-04 09:06'
updated_date: '2026-11-04 09:17'
labels:
  - jira
  - settings
dependencies: []
parent_task_id: TASK-121
priority: medium
ordinal: 211000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A Domain list of checks and their outcomes (passed, failed with a fix sentence, skipped because an earlier check failed), and the order they run in; tested.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Outcomes and fix sentences are tested
- [x] #2 A failed token check skips the checks that need it
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Domain/HealthCheck.cs (the six checks in order, states, HealthReport: a failed token check skips the rest, progress, summary) and Domain/HealthFixes.cs (fix sentences per status and check, the Work On Issues permission, time zone and clock rules), tested in HealthCheckTests and HealthFixesTests.
<!-- SECTION:FINAL_SUMMARY:END -->
