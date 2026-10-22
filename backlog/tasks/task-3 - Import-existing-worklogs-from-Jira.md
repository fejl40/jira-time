---
id: TASK-3
title: Import existing worklogs from Jira
status: To Do
assignee: []
created_date: 2026-10-19 08:59
updated_date: '2026-10-22T19:36:00Z'
labels: []
dependencies: []
priority: high
ordinal: 3000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Fill the timesheet with worklogs already logged in Jira for the selected profile, so hours logged elsewhere are visible and not double counted.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Worklogs for the current period are imported and linked by worklog id
- [ ] #2 Re-importing does not create duplicates
- [ ] #3 Imported entries are marked From Jira and count in totals, missing hours and suggestions
- [ ] #4 Import runs when a period opens and with Sync now; Jira being down only shows a notice
- [ ] #5 Covered by tests
<!-- AC:END -->
