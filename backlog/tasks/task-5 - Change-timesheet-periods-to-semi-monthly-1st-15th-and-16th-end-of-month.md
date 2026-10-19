---
id: TASK-5
title: Change timesheet periods to semi-monthly (1st-15th and 16th-end of month)
status: Done
assignee: []
created_date: '2026-10-19 09:15'
updated_date: '2026-10-19 09:56'
labels: []
dependencies: []
modified_files:
  - src/JiraTime/Domain/Documents.cs
  - src/JiraTime/Services/SettingsService.cs
  - src/JiraTime/Components/Pages/Timesheet.razor
  - src/JiraTime/Components/Pages/SettingsPage.razor
  - tests/JiraTime.Tests/PeriodTests.cs
  - tests/JiraTime.Tests/SettingsServiceTests.cs
  - README.md
type: enhancement
ordinal: 5000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Periods are currently 14 days repeating from an anchor date (Period.For + AppSettings.PeriodAnchor). The real periods are: the 1st to and including the 15th of the month, and the 16th to the last day of the month. Replace the 14-day logic, remove the period anchor setting from Settings, and make the grid handle periods of 13 to 16 days (the week 1 / week 2 split no longer applies as-is).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Any date maps to its period: 1-15 or 16-end of month, including February and leap years
- [x] #2 Next/previous navigation steps between half-month periods across month and year boundaries
- [x] #3 The timesheet grid shows the right number of days (13-16) with correct totals
- [x] #4 The period anchor setting is removed and old settings files still load
- [x] #5 Unit tests cover the period rules
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Period is now semi-monthly (1st-15th, 16th-end of month) via Period.For(date); the anchor setting and its Settings UI are gone, old settings files with periodAnchor still load. The grid sizes itself to the period (13-16 days) and draws week separators before Mondays. Verified in a browser against a fake Jira (Feb 13 days, Oct 15/16 days, navigation across months).
<!-- SECTION:FINAL_SUMMARY:END -->
