---
id: TASK-71.21
title: Read the selected day from TimesheetLink
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 96000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Timesheet parses the route and ?day= again instead of using Links.Read; the day fallback rule is untested. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 TimesheetLink.Selected(today) holds the fallback rule
- [ ] #2 Timesheet no longer calls Format.ParseIso
- [ ] #3 LinksTests cover the three fallback cases
<!-- AC:END -->
