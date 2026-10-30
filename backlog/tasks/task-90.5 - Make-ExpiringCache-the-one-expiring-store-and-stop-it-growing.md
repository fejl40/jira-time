---
id: TASK-90.5
title: Make ExpiringCache the one expiring store and stop it growing
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-90
priority: medium
ordinal: 144000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
ExpiringCache never removes expired entries, and the singleton JiraIssueLookup holds four of them for the server's lifetime. TimeLog clears by replacing its cache field, and RecentWorklogIssues hand-rolls its own expiry. Give ExpiringCache Clear(), Values (live entries) and pruning of expired entries on Set; TimeLog keeps a readonly cache and calls Clear(); RecentWorklogIssues keeps one ExpiringCache per owner.

Files:
- src/JiraTime/Services/ExpiringCache.cs
- src/JiraTime/Services/TimeLog.cs
- src/JiraTime/Services/RecentWorklogIssues.cs
- tests/JiraTime.Tests/ExpiringCacheTests.cs

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 No hand-rolled timestamp expiry outside ExpiringCache
- [ ] #2 Expired entries are removed from memory, with a test
- [ ] #3 TimeLog's cache field is readonly
- [ ] #4 ExpiringCacheTests cover Clear, Values and pruning
- [ ] #5 Only the files listed under Files are created, edited, moved or deleted
- [ ] #6 Behaviour and what the user sees stay the same
- [ ] #7 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
