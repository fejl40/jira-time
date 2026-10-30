---
id: TASK-90.11
title: Let PeriodSummary own unknown hours and work not in Jira
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 12:27'
labels:
  - refactor
dependencies:
  - TASK-90.10
parent_task_id: TASK-90
priority: medium
ordinal: 150000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The rule 'Jira unreadable means nothing is missing' is added by hand in PeriodHeader, PeriodReminders, PeriodGrid and PeriodLoader, and the not-in-Jira count is computed in Timesheet and PeriodLoader and passed down as Unsynced. Give PeriodSummary.For a hoursKnown input (Missing and StillToLog zero, IsComplete false when unknown), a NotInJira count, per-day MissingOn/LoggedOn used by PeriodSheet.Missing and Progress, and IsComplete(hasUnsaved). Remove the HoursKnown guards and the Unsynced parameters.

Files:
- src/JiraTime/Domain/PeriodSummary.cs
- src/JiraTime/Domain/PeriodSheet.cs
- src/JiraTime/Services/PeriodLoader.cs
- src/JiraTime/Components/Pages/Timesheet.razor
- src/JiraTime/Components/Sheet/ShownPeriod.cs
- src/JiraTime/Components/Sheet/PeriodHeader.razor
- src/JiraTime/Components/Sheet/PeriodReminders.razor
- src/JiraTime/Components/Sheet/PeriodGrid.razor
- tests/JiraTime.Tests/PeriodSummaryTests.cs
- tests/JiraTime.Tests/PeriodSheetTests.cs
- tests/JiraTime.Tests/PeriodLoaderTests.cs

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 No component combines HoursKnown with Missing or IsComplete, and none counts !IsSynced
- [x] #2 PeriodHeader and PeriodReminders have no Unsynced parameter
- [x] #3 PeriodSummaryTests cover unknown hours, NotInJira and IsComplete with unsaved work and work not in Jira
- [x] #4 Header, reminders, grid footer and previous-period banner behave as before when Jira is down
- [ ] #5 Only the files listed under Files are created, edited, moved or deleted
- [x] #6 Behaviour and what the user sees stay the same
- [x] #7 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Touched one file outside the list: src/JiraTime/Domain/SheetDay.cs (2 lines). SheetDay (made in TASK-90.x from PeriodSheet.Day) now holds Missing and Logged, so they read PeriodSummary.MissingOn/LoggedOn there instead of repeating the HoursKnown rule. AC 5 left unticked for that reason. StillToLog is NOT zeroed when hours are unknown, unlike the description: it is the expected hours of the days after today and doesn't depend on Jira, and the end-of-period reminder shows it today when Jira is down, so zeroing it would change what the user sees. PeriodSummary.For takes a fifth, optional hoursKnown parameter as the description asks. PeriodSheet caches its Summary until the next Load or the day changing, since SheetDay reads it per day. Browser check (main vs this, mock Jira up, down after a failed save, down after reload, synced again, an old period read while down): identical output.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
PeriodSummary owns the rules about unknown hours and work not in Jira: For(..., hoursKnown) makes Missing and MissingOn zero and IsComplete false when Jira couldn't be read; NotInJira counts the period's entries Jira doesn't have; LoggedOn/MissingOn/ExpectedOn give per-day values; IsComplete(hasUnsaved). PeriodSheet.Summary is cached per load and day; SheetDay.Missing/Logged read it. PeriodHeader, PeriodReminders and PeriodGrid lost their HoursKnown guards; PeriodHeader and PeriodReminders lost Unsynced; ShownPeriod.Unsynced is gone; PeriodLoader builds the previous period's OpenPeriod from the summary. Tests: unknown hours, NotInJira, per-day values, IsComplete with unsaved work and work not in Jira, the cached summary following loads and the clock. Doc changes for cleanup: AGENTS.md layout line for Domain/PeriodSummary.cs could read 'logged, missing (per day too), weekly hours and work not in Jira of a period; unknown hours when Jira can't be read'. Nothing else in AGENTS.md or README describes the removed parameters.
<!-- SECTION:FINAL_SUMMARY:END -->
