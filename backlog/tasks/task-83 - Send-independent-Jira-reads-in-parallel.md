---
id: TASK-83
title: Send independent Jira reads in parallel
status: To Do
assignee: []
created_date: '2026-10-30 10:05'
labels: []
dependencies:
  - TASK-81
ordinal: 132000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Period load runs sprint issues, period issues, worklogs and statuses one after another, and the per-issue worklog reads too. Run independent reads at the same time through the per-user gate (at most 4 at once). Writes stay one at a time, so per-issue write limits and the changed-in-Jira check keep working.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Sprint issues, period issues and worklogs are read at the same time when a period opens
- [ ] #2 Per-issue worklog reads run through the gate, at most 4 at once
- [ ] #3 Writes to Jira stay sequential
- [ ] #4 Tests show the reads overlap and the cap holds
<!-- AC:END -->
