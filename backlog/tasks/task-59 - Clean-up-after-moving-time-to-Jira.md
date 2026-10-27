---
id: TASK-59
title: Clean up after moving time to Jira
status: To Do
assignee: []
created_date: '2026-10-27 14:02'
labels:
  - jira-source-of-truth
  - refactor
dependencies:
  - TASK-54
  - TASK-55
  - TASK-56
  - TASK-57
priority: medium
ordinal: 59000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Remove what local time storage left behind: sync fields on TimeEntry (SyncPending, DeletePending, FromJira, Owner and others no longer needed), the import and matching code, TimeEntryService's store queries. Update README (how it works, Where data lives) and the tests.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 No code reads or writes data/documents/time-entries except the one-time move
- [ ] #2 Unused sync and import code and fields are gone
- [ ] #3 README describes Jira as the source of truth and lists what is still stored locally
- [ ] #4 Build and tests pass
<!-- AC:END -->
