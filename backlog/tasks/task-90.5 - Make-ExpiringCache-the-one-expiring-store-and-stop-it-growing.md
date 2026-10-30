---
id: TASK-90.5
title: Make ExpiringCache the one expiring store and stop it growing
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 11:35'
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
- [x] #1 No hand-rolled timestamp expiry outside ExpiringCache
- [x] #2 Expired entries are removed from memory, with a test
- [x] #3 TimeLog's cache field is readonly
- [x] #4 ExpiringCacheTests cover Clear, Values and pruning
- [x] #5 Only the files listed under Files are created, edited, moved or deleted
- [x] #6 Behaviour and what the user sees stay the same
- [x] #7 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
ExpiringCache now has Clear(), Values (live values only), an optional key comparer, and drops expired entries on every Set, so the singleton JiraIssueLookup caches no longer grow for the server's lifetime. A value that was loading while the cache was cleared is not stored afterwards (matches the old replace-the-field behaviour of TimeLog.Refresh). TimeLog keeps a readonly cache and Refresh() calls Clear(). RecentWorklogIssues keeps one ExpiringCache<JiraIssue> per owner (case-insensitive keys) instead of its own timestamps. New ExpiringCacheTests: Clear, Values, pruning from memory, loads across Clear, case-insensitive keys. Doc changes for cleanup: AGENTS.md layout line for Services/ExpiringCache.cs could read 'small cache whose values expire; expired values are dropped as new ones are stored'; the Tests paragraph could mention the expiring cache. No other doc changes needed.
<!-- SECTION:FINAL_SUMMARY:END -->
