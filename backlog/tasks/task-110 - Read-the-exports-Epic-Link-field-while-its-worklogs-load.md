---
id: TASK-110
title: Read the export's Epic Link field while its worklogs load
status: To Do
assignee: []
created_date: '2026-10-30 15:19'
labels:
  - jira
  - performance
dependencies: []
priority: medium
ordinal: 175000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The month export reads the month's worklogs, then the Epic Link field (once per user, cached), then the epics. The field doesn't depend on the worklogs, so ask for it at the same time as the worklog read; only the epic lookup has to wait for the issues.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Opening the export asks for the Epic Link field at the same time as the month's worklogs (when it isn't cached)
- [ ] #2 Tests show the requests overlap and the export is unchanged
- [ ] #3 Build has 0 warnings and all tests pass
<!-- AC:END -->
