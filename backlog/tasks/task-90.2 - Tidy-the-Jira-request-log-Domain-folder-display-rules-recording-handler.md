---
id: TASK-90.2
title: 'Tidy the Jira request log: Domain folder, display rules, recording handler'
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 11:36'
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
- [x] #1 The three request-log models are in Domain/JiraLog/
- [x] #2 State, ShortPath and Outcome exist once, in Domain, with tests (API v2/v3 paths, query stripping, pending, failed, status with error)
- [x] #3 JiraHttp.cs holds no logging types and each JiraLog file holds one concept
- [x] #4 Markup and data-* attributes of toasts and log groups are unchanged
- [x] #5 Only the files listed under Files are created, edited, moved or deleted
- [x] #6 Behaviour and what the user sees stay the same
- [x] #7 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Moved JiraLog.cs, JsonBody.cs and RequestToasts.cs into Domain/JiraLog/ (namespace JiraTime.Domain unchanged). JiraLogEntry now has State, ShortPath and Outcome(duration) (not written to the log file); JiraToasts and LogGroups use them, JiraToasts uses Clock.Today(), and the new RequestDot.razor draws the pending spinner and ok/error dot (InBadge: the Logs badge's spinner and status code). JiraRequest and JiraRecordingHandler moved to Services/JiraLog/JiraRecordingHandler.cs; JiraException got a summary. Tests for State, ShortPath (v2/v3, query, other paths) and Outcome added to JiraLogDayTests; RequestToastsTests needed no change. Rendered markup and data-* attributes are the same. Doc changes for cleanup: AGENTS.md Layout - replace the three lines Domain/JiraLog.cs, Domain/JsonBody.cs, Domain/RequestToasts.cs with one 'Domain/JiraLog/  the Jira request log: one logged request (JiraLogEntry, with its state, short path and outcome), the Logs page's day, groups and filters (JiraLog.cs), a body as a JSON tree (JsonBody.cs), the live request toasts (RequestToasts.cs)'; Services/JiraLog/ line: add 'the recording handler and JiraRequest (JiraRecordingHandler.cs)'; Components/Logs/ line: add 'the request dot (RequestDot)'. In 'Jira request log', JiraRecordingHandler is now in Services/JiraLog/JiraRecordingHandler.cs (text still correct).
<!-- SECTION:FINAL_SUMMARY:END -->
