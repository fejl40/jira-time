---
id: TASK-90.10
title: >-
  Make CurrentUser the only source of the Jira connection and take Jira search
  out of components
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
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
- [ ] #1 No .razor file injects JiraClient or JiraWorklogClient
- [ ] #2 No public method on a scoped service takes JiraConnection
- [ ] #3 The Connection/Profile guard pattern appears only in CurrentUser
- [ ] #4 DayEditor, DayRow and IssuePicker have no Connection or Search parameter and statuses are not a regular parameter
- [ ] #5 Search requests are still logged under 'Search issues'
- [ ] #6 Only the files listed under Files are created, edited, moved or deleted
- [ ] #7 Behaviour and what the user sees stay the same
- [ ] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
