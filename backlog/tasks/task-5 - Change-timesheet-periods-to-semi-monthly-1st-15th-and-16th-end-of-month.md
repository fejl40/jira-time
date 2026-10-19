---
id: TASK-5
title: Change timesheet periods to semi-monthly (1st-15th and 16th-end of month)
status: To Do
assignee: []
created_date: '2026-10-19 09:15'
labels: []
dependencies: []
type: enhancement
ordinal: 5000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Periods are currently 14 days repeating from an anchor date (Period.For + AppSettings.PeriodAnchor). The real periods are: the 1st to and including the 15th of the month, and the 16th to the last day of the month. Replace the 14-day logic, remove the period anchor setting from Settings, and make the grid handle periods of 13 to 16 days (the week 1 / week 2 split no longer applies as-is).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Any date maps to its period: 1-15 or 16-end of month, including February and leap years
- [ ] #2 Next/previous navigation steps between half-month periods across month and year boundaries
- [ ] #3 The timesheet grid shows the right number of days (13-16) with correct totals
- [ ] #4 The period anchor setting is removed and old settings files still load
- [ ] #5 Unit tests cover the period rules
<!-- AC:END -->
