---
id: TASK-90.11
title: Let PeriodSummary own unknown hours and work not in Jira
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
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
- [ ] #1 No component combines HoursKnown with Missing or IsComplete, and none counts !IsSynced
- [ ] #2 PeriodHeader and PeriodReminders have no Unsynced parameter
- [ ] #3 PeriodSummaryTests cover unknown hours, NotInJira and IsComplete with unsaved work and work not in Jira
- [ ] #4 Header, reminders, grid footer and previous-period banner behave as before when Jira is down
- [ ] #5 Only the files listed under Files are created, edited, moved or deleted
- [ ] #6 Behaviour and what the user sees stay the same
- [ ] #7 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
