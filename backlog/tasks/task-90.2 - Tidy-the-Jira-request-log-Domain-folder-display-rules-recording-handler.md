---
id: TASK-90.2
title: 'Tidy the Jira request log: Domain folder, display rules, recording handler'
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-90
priority: medium
ordinal: 141000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Group the request-log models (JiraLog.cs, JsonBody.cs, RequestToasts.cs) in Domain/JiraLog/ keeping the namespace JiraTime.Domain so no usings change. Move the duplicated display rules State, ShortPath and Outcome from JiraToasts.razor and LogGroups.razor into JiraLogEntry (Outcome takes the formatted duration so Domain doesn't use Components); JiraToasts uses Clock.Today(); add a small RequestDot.razor for the shared pending/ok/error dot. Move JiraRequest and JiraRecordingHandler out of JiraHttp.cs and JiraRequestRecorder.cs into Services/JiraLog/JiraRecordingHandler.cs; JiraException gets a summary.

Files:
- src/JiraTime/Domain/JiraLog.cs -> src/JiraTime/Domain/JiraLog/JiraLog.cs
- src/JiraTime/Domain/JsonBody.cs -> src/JiraTime/Domain/JiraLog/JsonBody.cs
- src/JiraTime/Domain/RequestToasts.cs -> src/JiraTime/Domain/JiraLog/RequestToasts.cs
- src/JiraTime/Components/Logs/JiraToasts.razor
- src/JiraTime/Components/Logs/LogGroups.razor
- src/JiraTime/Components/Logs/RequestDot.razor (new)
- src/JiraTime/Services/Jira/JiraHttp.cs
- src/JiraTime/Services/JiraLog/JiraRequestRecorder.cs
- src/JiraTime/Services/JiraLog/JiraRecordingHandler.cs (new)
- tests/JiraTime.Tests/JiraLogDayTests.cs
- tests/JiraTime.Tests/RequestToastsTests.cs

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 The three request-log models are in Domain/JiraLog/
- [ ] #2 State, ShortPath and Outcome exist once, in Domain, with tests (API v2/v3 paths, query stripping, pending, failed, status with error)
- [ ] #3 JiraHttp.cs holds no logging types and each JiraLog file holds one concept
- [ ] #4 Markup and data-* attributes of toasts and log groups are unchanged
- [ ] #5 Only the files listed under Files are created, edited, moved or deleted
- [ ] #6 Behaviour and what the user sees stay the same
- [ ] #7 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
