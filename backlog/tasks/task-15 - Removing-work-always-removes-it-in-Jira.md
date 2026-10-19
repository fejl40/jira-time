---
id: TASK-15
title: Removing work always removes it in Jira
status: Done
assignee: []
created_date: '2026-10-19 12:41'
updated_date: '2026-10-19 12:44'
labels: []
dependencies: []
priority: high
type: feature
ordinal: 15000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
When work is removed in Jira Time, its worklog must be deleted in Jira so the issue's logged hours go down. If Jira can't be reached at that moment, the removal is remembered and retried (Sync now), instead of the entry silently staying or the worklog being left behind.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Removing synced work deletes its worklog in Jira
- [x] #2 If the Jira delete fails, the work disappears from totals but is kept as a pending removal, shown as not in Jira and retried by Sync now
- [x] #3 A worklog already deleted in Jira counts as removed
- [x] #4 Work never sent to Jira is removed locally only
- [x] #5 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
TimeEntry gets DeletePending. WorklogSync.DeleteAsync deletes the worklog in Jira (on the issue Jira has it on; a 404 counts as gone) and then the entry. If Jira can't be reached, the entry is kept as a pending removal: hidden from day/period totals, recent issues and the 24 h check, but counted under "not in Jira" (GetUnsyncedAsync) and finished by SyncAsync / Sync now. Work never sent to Jira is just removed locally. Tests cover online delete, offline delete then retry, already-deleted worklogs, moved work and the 24 h limit; checked in the browser with the mock Jira switched down and up.
<!-- SECTION:FINAL_SUMMARY:END -->
