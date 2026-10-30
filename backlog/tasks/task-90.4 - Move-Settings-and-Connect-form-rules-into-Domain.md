---
id: TASK-90.4
title: Move Settings and Connect form rules into Domain
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-90
priority: low
ordinal: 143000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
SettingsPage.razor parses and validates work hours itself and Connect.razor normalises the Jira URL in the page; neither is tested. Add WorkWeekInput.Parse in Domain/WorkCalendar.cs (hours or the first day's error, plus Total) and JiraConnection.NormalizeUrl in Domain/Jira.cs. Move Pages/Setup/Connect.razor to Pages/Connect.razor (route /setup/connect stays) so the one-file Setup folder goes. Split WorkCalendarTests into Domain tests and WorkCalendarServiceTests with one shared harness.

Files:
- src/JiraTime/Domain/WorkCalendar.cs
- src/JiraTime/Domain/Jira.cs
- src/JiraTime/Components/Pages/SettingsPage.razor
- src/JiraTime/Components/Pages/Setup/Connect.razor -> src/JiraTime/Components/Pages/Connect.razor
- tests/JiraTime.Tests/WorkCalendarTests.cs
- tests/JiraTime.Tests/WorkCalendarServiceTests.cs (new)
- tests/JiraTime.Tests/JiraConnectionTests.cs (new)

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 The pages contain no parsing or URL logic
- [ ] #2 Tests cover 7:30 and 0 accepted, the error naming the right weekday, and URLs without scheme, with a trailing slash and invalid
- [ ] #3 The Setup folder is gone
- [ ] #4 WorkCalendarTests holds only Domain tests; service tests are in WorkCalendarServiceTests
- [ ] #5 User-facing messages are unchanged
- [ ] #6 Only the files listed under Files are created, edited, moved or deleted
- [ ] #7 Behaviour and what the user sees stay the same
- [ ] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
