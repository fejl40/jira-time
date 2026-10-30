---
id: TASK-91
title: Keep the Save all message when it opens another day
status: Done
assignee: []
created_date: '2026-10-30 13:00'
updated_date: '2026-10-30 13:04'
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
- [x] #1 After Save all, the message saying why a day wasn't saved shows on that day, also when another day was on screen
- [x] #2 The message still goes away when the user picks another day or period, saves again or discards all
- [x] #3 A test covers Save all opening another day with its message
- [x] #4 Browser check: Save all with an unsaveable day that isn't on screen shows the message on that day
- [x] #5 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
ShownPeriod remembers the day Save all sends the user to (_noticeDay); opening that day keeps the notice, any other day, period, save or discard all clears it. Test in ShownPeriodTests. Browser check: Save all from Tuesday with unreadable hours on Monday opens Monday with '1 day not saved because a row needs an issue or hours…' (before: no message).
<!-- SECTION:FINAL_SUMMARY:END -->
