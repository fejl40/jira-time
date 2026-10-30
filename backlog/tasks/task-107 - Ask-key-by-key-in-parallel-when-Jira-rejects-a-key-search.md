---
id: TASK-107
title: Ask key by key in parallel when Jira rejects a key search
status: To Do
assignee: []
created_date: '2026-10-30 15:19'
labels:
  - jira
  - performance
dependencies: []
priority: medium
ordinal: 172000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
When a search by keys gets 400 (Cloud, which has no validateQuery, and one key unknown), JiraSearch asks key by key, one at a time. Send those searches together (the client's limiter caps how many are in flight). Unknown keys are still skipped.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 The key-by-key fallback sends its searches at the same time, in batches the limiter allows
- [ ] #2 Unknown keys are still skipped and found issues keep their order
- [ ] #3 Tests show the requests overlap and the results are unchanged
- [ ] #4 Build has 0 warnings and all tests pass
<!-- AC:END -->
