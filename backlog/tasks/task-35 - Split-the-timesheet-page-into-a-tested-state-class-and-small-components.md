---
id: TASK-35
title: Split the timesheet page into a tested state class and small components
status: Done
assignee: []
created_date: '2026-10-27 09:03'
updated_date: '2026-10-27 09:22'
labels: []
dependencies: []
ordinal: 35000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Timesheet.razor is 874 lines that mix period loading, unsaved-work bookkeeping, suggestions, time off and all the markup. Move the state and rules into a plain C# class with unit tests, and the markup into focused components.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Period state and unsaved-work rules live in a plain class with unit tests
- [x] #2 The header, reminders, day list, grid and day header are separate components
- [x] #3 The page only wires services, navigation and keyboard shortcuts
- [x] #4 Behaviour is unchanged (checked in the browser)
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
PeriodSheet (Domain) holds the period's saved data, unsaved changes per day and the rules (missing hours, suggestions, fill period, unsaved totals, grid rows, cell clicks), with 8 tests. The markup moved to Components/Sheet: PeriodHeader (with the Save all question), PeriodReminders, DayList, PeriodGrid and DayHeader (with time off). Timesheet.razor went from 874 to about 380 lines and only loads data and wires services, navigation and shortcuts. Checked in the browser against the fake Jira: fill, save all, discard, removal count, cell click focus, time off, arrow keys, leave prompt and the phone day list.
<!-- SECTION:FINAL_SUMMARY:END -->
