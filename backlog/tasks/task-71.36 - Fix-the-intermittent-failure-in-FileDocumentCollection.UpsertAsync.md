---
id: TASK-71.36
title: Fix the intermittent failure in FileDocumentCollection.UpsertAsync
status: To Do
assignee: []
created_date: '2026-10-29 08:25'
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
- [ ] #1 The cause is found and explained in the final summary
- [ ] #2 A fix with a test that would have caught it (or a stress test that runs it many times)
- [ ] #3 dotnet test passes repeatedly (e.g. 20 runs)
<!-- AC:END -->
