---
id: TASK-71.21
title: Read the selected day from TimesheetLink
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:00'
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
- [x] #1 TimesheetLink.Selected(today) holds the fallback rule
- [x] #2 Timesheet no longer calls Format.ParseIso
- [x] #3 LinksTests cover the three fallback cases
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
TimesheetLink.Selected(today) holds the day fallback rule and Timesheet reads its period and day with Links.Read instead of parsing the route itself; LinksTests cover the three cases.
<!-- SECTION:FINAL_SUMMARY:END -->
