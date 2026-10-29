---
id: TASK-71.36
title: Fix the intermittent failure in FileDocumentCollection.UpsertAsync
status: Done
assignee: []
created_date: '2026-10-29 08:25'
updated_date: '2026-10-29 08:31'
labels:
  - bug
dependencies: []
parent_task_id: TASK-71
priority: high
ordinal: 111000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
During the TASK-71 work, JiraIdentityTests.When_Jira_is_unreachable_a_token_confirmed_before_still_works(serverError: False) failed once with a NullReferenceException in FileDocumentCollection.UpsertAsync and passed on later runs. Find the cause (likely a race in the in-memory cache or file reload) and fix it.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 The cause is found and explained in the final summary
- [x] #2 A fix with a test that would have caught it (or a stress test that runs it many times)
- [x] #3 dotnet test passes repeatedly (e.g. 20 runs)
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Cause: UpsertAsync checked _cache for null and then indexed it, while the file watcher (fired by that same File.Move) set _cache to null on its own thread in between; readers also enumerated the dictionary that writes mutated. Fix: the cache is an immutable snapshot swapped with CompareExchange (writes copy it, a watcher drop always wins, a load that raced a disk change is used once but not kept); a concurrent stress test and a hand-edit reload test were added, and dotnet test passed 20 runs in a row.
<!-- SECTION:FINAL_SUMMARY:END -->
