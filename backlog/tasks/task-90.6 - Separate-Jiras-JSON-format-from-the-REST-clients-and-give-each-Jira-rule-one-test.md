---
id: TASK-90.6
title: >-
  Separate Jira's JSON format from the REST clients and give each Jira rule one
  test
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
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
- [ ] #1 No TimeZoneInfo.Local in src
- [ ] #2 The REST clients contain no JSON-to-domain mapping and the JSON helpers are internal or private
- [ ] #3 Each rule has one dedicated test: embedded worklog overflow, Cloud v3 body, started format
- [ ] #4 No test simulates the unknown-key 400 with FailWhen
- [ ] #5 The started string sent to Jira is unchanged
- [ ] #6 Only the files listed under Files are created, edited, moved or deleted
- [ ] #7 Behaviour and what the user sees stay the same
- [ ] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
