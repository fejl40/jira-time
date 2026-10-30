---
id: TASK-90.6
title: >-
  Separate Jira's JSON format from the REST clients and give each Jira rule one
  test
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 11:57'
labels:
  - refactor
dependencies:
  - TASK-90.1
parent_task_id: TASK-90
priority: medium
ordinal: 145000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
JiraWorklogClient and JiraClient mix HTTP calls with JSON-to-domain mapping (worklog parsing, started/created formats, request body, epic and profile mapping), and FormatStarted falls back to TimeZoneInfo.Local, a hidden server dependency. Move the mapping into internal Services/Jira/JiraWorklogJson.cs and JiraIssueJson.cs; JiraWorklogClient takes TimeProvider and passes its LocalTimeZone; drop the redundant _jira fields. In tests: create JiraWorklogClientTests for the embedded-worklog overflow rule, page reading, the Cloud v3 body and FormatStarted; delete the duplicate overflow tests; stop simulating the unknown-key 400 with FailWhen in PeriodIssuesTests and EpicLookupTests (FakeJira models it now).

Files:
- src/JiraTime/Services/JiraWorklogClient.cs
- src/JiraTime/Services/JiraClient.cs
- src/JiraTime/Services/Jira/JiraDtos.cs
- src/JiraTime/Services/Jira/JiraWorklogJson.cs (new)
- src/JiraTime/Services/Jira/JiraIssueJson.cs (new)
- src/JiraTime/Services/Jira/JiraResilience.cs (only if registration changes)
- tests/JiraTime.Tests/JiraWorklogClientTests.cs (new)
- tests/JiraTime.Tests/SaveDayTests.cs
- tests/JiraTime.Tests/PeriodLoaderTests.cs
- tests/JiraTime.Tests/TimeLogTests.cs
- tests/JiraTime.Tests/PeriodIssuesTests.cs
- tests/JiraTime.Tests/EpicLookupTests.cs
- tests/JiraTime.Tests/TimeHarness.cs

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 No TimeZoneInfo.Local in src
- [x] #2 The REST clients contain no JSON-to-domain mapping and the JSON helpers are internal or private
- [x] #3 Each rule has one dedicated test: embedded worklog overflow, Cloud v3 body, started format
- [x] #4 No test simulates the unknown-key 400 with FailWhen
- [x] #5 The started string sent to Jira is unchanged
- [x] #6 Only the files listed under Files are created, edited, moved or deleted
- [x] #7 Behaviour and what the user sees stay the same
- [x] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Moved Jira's JSON-to-domain mapping out of the REST clients into internal Services/Jira/JiraWorklogJson.cs (worklog reading, started/created formats, whole minutes, author check, worklog body, WorklogRange) and Services/Jira/JiraIssueJson.cs (profile, issue with status, picker issues, epic and Epic Link field mapping). JiraWorklogClient takes TimeProvider and formats started with clock.LocalTimeZone (TimeProvider.System's zone is TimeZoneInfo.Local, so the string sent is unchanged); both clients use their primary-constructor JiraHttp directly. New JiraWorklogClientTests: embedded worklog overflow, page-by-page reads, Cloud v3 body, started format (per time zone), whole minutes and created parsing, all through the client. Removed the duplicate overflow tests from PeriodLoaderTests and TimeLogTests and the FailWhen 400 simulations from PeriodIssuesTests and EpicLookupTests; TestClock got a settable Zone. JiraDtos.cs and JiraResilience.cs needed no change. SaveAllTests keeps its overflow test for the check before saving (not in this subtask's files). Docs for cleanup: AGENTS.md layout line for Services/Jira/ should mention JiraWorklogJson and JiraIssueJson (Jira's JSON mapped to the app's worklogs, issues, profile and epics); the Logging a day note can say the 09:00 start is sent with the server time zone's offset (TimeProvider); Tests list can mention reading worklogs in Jira's format (embedded worklogs, pages, worklog body).
<!-- SECTION:FINAL_SUMMARY:END -->
