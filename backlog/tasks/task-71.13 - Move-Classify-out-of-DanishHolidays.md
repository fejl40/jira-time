---
id: TASK-71.13
title: Move Classify out of DanishHolidays
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 88000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
DanishHolidays.Classify decides a day's kind from work hours and days off, which is a work-calendar rule, not part of the holiday list. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 DayInfo.For replaces DanishHolidays.Classify
- [ ] #2 DanishHolidays has no reference to UserSettings or DayOff
- [ ] #3 WorkCalendarTests unchanged and passing
<!-- AC:END -->
