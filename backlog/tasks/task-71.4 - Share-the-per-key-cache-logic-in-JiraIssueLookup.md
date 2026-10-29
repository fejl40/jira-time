---
id: TASK-71.4
title: Share the per-key cache logic in JiraIssueLookup
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:01'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 79000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
GetStatusesAsync and GetEpicsAsync repeat the cache key, dedupe, find-missing, load and read-back steps. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Cache-key format and missing-keys logic exist once
- [x] #2 Public signatures and results unchanged; tests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
GetStatusesAsync and GetEpicsAsync share one PerKeyAsync helper for the cache key, dedupe, missing keys, load and read-back.
<!-- SECTION:FINAL_SUMMARY:END -->
