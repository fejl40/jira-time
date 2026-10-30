---
id: TASK-106
title: Read an old period and the recent days together
status: Done
assignee: []
created_date: '2026-10-30 15:19'
updated_date: '2026-10-30 15:23'
labels:
  - jira
  - performance
dependencies: []
priority: medium
ordinal: 171000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Opening a period far from today reads the period's worklogs and then the last 60 days (for recent picks) one after the other. They don't depend on each other: send both at once.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Opening an old period sends its worklog search and the recent days' search at the same time
- [x] #2 Tests show the requests overlap and the results are unchanged
- [x] #3 Build has 0 warnings and all tests pass
<!-- AC:END -->
