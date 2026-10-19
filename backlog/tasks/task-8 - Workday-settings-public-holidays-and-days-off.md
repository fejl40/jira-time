---
id: TASK-8
title: 'Workday settings, public holidays and days off'
status: Done
assignee: []
created_date: '2026-10-19 11:44'
updated_date: '2026-10-19 11:58'
labels: []
dependencies: []
modified_files:
  - src/JiraTime/Domain/WorkCalendar.cs
  - src/JiraTime/Domain/Documents.cs
  - src/JiraTime/Services/WorkCalendarService.cs
  - src/JiraTime/Services/SettingsService.cs
  - src/JiraTime/Components/Pages/Timesheet.razor
  - src/JiraTime/Components/Pages/SettingsPage.razor
  - src/JiraTime/Program.cs
  - tests/JiraTime.Tests/WorkCalendarTests.cs
  - tests/JiraTime.Tests/SettingsServiceTests.cs
priority: high
type: feature
ordinal: 8000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Define which days need 8 hours and when the workday runs, so the timesheet can show how many hours are missing and Fill knows where the gaps are. Mon-Fri are workdays at 8 h. Danish public holidays are skipped automatically. Carl can mark a day off (vacation, sick, other). Workday window is a setting, default 08:00-16:30 with an unlogged lunch 12:00-12:30.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Settings has workday start, end and lunch start/end with the defaults above
- [x] #2 Danish public holidays (incl. Easter-based ones) are computed per year and are not workdays
- [x] #3 A day can be marked off and unmarked from the day view
- [x] #4 The grid shows each workday's missing hours and the period's missing total
- [x] #5 Unit tests cover the holiday calendar and the expected-hours calculation
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Workday (start, end, lunch) is a setting with defaults 08:00-16:30 and lunch 12:00-12:30, validated and editable in Settings. DanishHolidays computes the official holidays per year (Easter-based ones via the Gregorian algorithm; Great Prayer Day only before 2024). Days off (Vacation/Sick/Other) are stored in a days-off collection and set from the day view. The grid greys out holidays/days off with the name as tooltip, adds a Missing row per workday up to today, and the header shows the period's missing hours. 22 new tests.
<!-- SECTION:FINAL_SUMMARY:END -->
