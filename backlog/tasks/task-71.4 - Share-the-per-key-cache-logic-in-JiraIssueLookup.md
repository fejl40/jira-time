---
id: TASK-71.4
title: Share the per-key cache logic in JiraIssueLookup
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
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
- [ ] #1 Cache-key format and missing-keys logic exist once
- [ ] #2 Public signatures and results unchanged; tests pass
<!-- AC:END -->
