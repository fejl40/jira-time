---
id: TASK-90.4
title: Move Settings and Connect form rules into Domain
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 11:36'
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
- [x] #1 The pages contain no parsing or URL logic
- [x] #2 Tests cover 7:30 and 0 accepted, the error naming the right weekday, and URLs without scheme, with a trailing slash and invalid
- [x] #3 The Setup folder is gone
- [x] #4 WorkCalendarTests holds only Domain tests; service tests are in WorkCalendarServiceTests
- [x] #5 User-facing messages are unchanged
- [x] #6 Only the files listed under Files are created, edited, moved or deleted
- [x] #7 Behaviour and what the user sees stay the same
- [x] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Added WorkWeekInput (Days, Parse, Total) in Domain/WorkCalendar.cs and JiraConnection.NormalizeUrl in Domain/Jira.cs; SettingsPage and Connect now call them, with the same messages. Connect.razor moved from Pages/Setup/ to Pages/ (route /setup/connect unchanged), so the Setup folder is gone. WorkCalendarTests now holds only Domain tests (plus WorkWeekInput parsing); the WorkCalendarService tests moved to WorkCalendarServiceTests, sharing one TimeHarness per test class instance. New JiraConnectionTests cover URLs without scheme, with trailing slashes and invalid. Doc changes for cleanup: AGENTS.md Layout - replace 'Components/Pages/Setup/            Connect' with 'Components/Pages/Connect.razor     connecting to Jira (route /setup/connect)'; Domain/WorkCalendar.cs line could add 'work hours as typed on Settings (WorkWeekInput)'; Domain/Jira.cs line could add 'the typed Jira URL (NormalizeUrl)'. Tests section: mention the Settings work hours and the Jira URL from Connect.
<!-- SECTION:FINAL_SUMMARY:END -->
