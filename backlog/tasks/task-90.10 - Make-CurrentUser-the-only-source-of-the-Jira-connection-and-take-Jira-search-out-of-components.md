---
id: TASK-90.10
title: >-
  Make CurrentUser the only source of the Jira connection and take Jira search
  out of components
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 12:17'
labels:
  - refactor
dependencies:
  - TASK-90.7
  - TASK-90.9
  - TASK-90.5
  - TASK-90.6
parent_task_id: TASK-90
priority: high
ordinal: 149000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Some services read the connection from CurrentUser with a hand-written guard repeated four times, while WorklogSync takes a JiraConnection from the page and DayEditor injects JiraClient and threads a Connection and a Search delegate through DayRow into IssuePicker; IssueStatuses is passed as a component parameter because it is transient. Add CurrentUser.Jira (a SignedInJira record) used by every scoped service; WorklogSync takes CurrentUser and drops JiraConnection parameters; the issue search moves into a scoped service (IssueStatuses.SearchAsync, logged as 'Search issues "..."'); IssuePicker uses it directly; DayEditor and DayRow lose Connection and Search; Timesheet shares IssueStatuses as a fixed cascading value; IssuePicker builds its groups once per render.

Files:
- src/JiraTime/Services/CurrentUser.cs
- src/JiraTime/Services/WorklogSync.cs
- src/JiraTime/Services/TimeLog.cs
- src/JiraTime/Services/PeriodLoader.cs
- src/JiraTime/Services/MonthExportLoader.cs
- src/JiraTime/Services/IssueStatuses.cs
- src/JiraTime/Components/Pages/Timesheet.razor
- src/JiraTime/Components/Sheet/ShownPeriod.cs
- src/JiraTime/Components/Sheet/DayPanel.razor
- src/JiraTime/Components/Sheet/DayEditor.razor
- src/JiraTime/Components/Sheet/DayRow.razor
- src/JiraTime/Components/Sheet/IssuePicker.razor
- tests/JiraTime.Tests/TimeHarness.cs
- tests/JiraTime.Tests/SaveAllTests.cs
- tests/JiraTime.Tests/RetryQueueTests.cs
- tests/JiraTime.Tests/SaveDayTests.cs
- tests/JiraTime.Tests/JiraActionTests.cs
- tests/JiraTime.Tests/ParallelReadsTests.cs
- tests/JiraTime.Tests/JiraRequestLogTests.cs
- tests/JiraTime.Tests/JiraResilienceTests.cs
- tests/JiraTime.Tests/TimeLogTests.cs
- tests/JiraTime.Tests/ShownPeriodTests.cs

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 No .razor file injects JiraClient or JiraWorklogClient
- [x] #2 No public method on a scoped service takes JiraConnection
- [ ] #3 The Connection/Profile guard pattern appears only in CurrentUser
- [x] #4 DayEditor, DayRow and IssuePicker have no Connection or Search parameter and statuses are not a regular parameter
- [x] #5 Search requests are still logged under 'Search issues'
- [x] #6 Only the files listed under Files are created, edited, moved or deleted
- [x] #7 Behaviour and what the user sees stay the same
- [x] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
AC #3: the four service guards (TimeLog, PeriodLoader, IssueStatuses, MonthExportLoader) now read CurrentUser.Jira; the only Connection/Profile pattern left is in Components/Layout/MainLayout.razor (User is { IsSignedIn: true, Connection: { } c, Profile: { } p }), which is outside this subtask's files. Left for the cleanup: change it to User.Jira is { } jira. SettingsPage (User.Connection is { } c) and ExportPage (User.Connection is null) could read User.Jira the same way.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
CurrentUser.Jira (SignedInJira: connection + profile) is the one source of the Jira connection; Connection and Profile are read through it, and RequireJira() throws a JiraException when nobody is signed in. TimeLog, PeriodLoader, IssueStatuses and MonthExportLoader read CurrentUser.Jira instead of their own guards. WorklogSync takes CurrentUser: SaveDaysAsync(plans) and SyncRangeAsync(from, to) no longer take a JiraConnection. The issue search moved into IssueStatuses.SearchAsync (logged as Search issues “…”); Timesheet shares the page's IssueStatuses as a fixed cascading value, IssuePicker searches through it and DayEditor looks up a picked issue's status with it, so DayEditor and DayRow lost Connection, Search and OnIssuePicked and no razor file injects JiraClient. IssuePicker builds its groups once per render and the arrow keys move through the groups shown. Tests: search logged under its action, no Jira request without a signed-in user. Doc changes for cleanup: AGENTS.md layout line for Services/CurrentUser.cs -> 'the verified user of this tab and its Jira connection (CurrentUser.Jira); data services filter by it'; Services/IssueStatuses.cs -> 'current status of the issues a timesheet shows, and the issue picker's Jira search'; Services/JiraLog line unchanged. MainLayout.razor still has the Connection/Profile pattern (see notes).
<!-- SECTION:FINAL_SUMMARY:END -->
