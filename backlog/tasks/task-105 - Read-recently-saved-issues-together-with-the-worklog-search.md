---
id: TASK-105
title: Read recently saved issues together with the worklog search
status: To Do
assignee: []
created_date: '2026-10-30 15:19'
labels:
  - jira
  - performance
dependencies: []
priority: medium
ordinal: 170000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
For 15 minutes after a save, GetMyWorklogsAsync reads the issues just saved on by key only after the worklog search has answered, though the two don't depend on each other (the search result is only used to leave out issues it already found). Send both at once and merge, issues found by both counted once.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 The worklog search and the search by key for recently saved issues go out at the same time
- [ ] #2 An issue both return is read and shown once
- [ ] #3 Tests show the requests overlap and the results are unchanged
- [ ] #4 Build has 0 warnings and all tests pass
<!-- AC:END -->
