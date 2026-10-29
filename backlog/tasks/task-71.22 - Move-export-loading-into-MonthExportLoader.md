---
id: TASK-71.22
title: Move export loading into MonthExportLoader
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 97000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
ExportPage.LoadAsync loads time, epics and values and works out rules in the page, untested. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A MonthExportLoader service returns rows, values, no-epic issues, not-in-Jira count and Jira error
- [ ] #2 ExportPage @code is about 60 lines
- [ ] #3 Loader tests through TimeHarness cover a Jira failure and pending entries
<!-- AC:END -->
