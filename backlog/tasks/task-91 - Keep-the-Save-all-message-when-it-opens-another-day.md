---
id: TASK-91
title: Keep the Save all message when it opens another day
status: To Do
assignee: []
created_date: '2026-10-30 13:00'
labels:
  - bug
dependencies: []
priority: medium
ordinal: 156000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
After Save all, when some days couldn't be saved, the timesheet opens the first of them and shows a message saying why (ShownPeriod.Notice). When that day isn't the one on screen, the page selects it through the URL, and ShownPeriod.ShowDay clears the notice as the day opens, so the user never sees why the day wasn't saved. The message should stay until the user moves on themselves (picks another day or period, saves again, or discards all).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 After Save all, the message saying why a day wasn't saved shows on that day, also when another day was on screen
- [ ] #2 The message still goes away when the user picks another day or period, saves again or discards all
- [ ] #3 A test covers Save all opening another day with its message
- [ ] #4 Browser check: Save all with an unsaveable day that isn't on screen shows the message on that day
- [ ] #5 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
