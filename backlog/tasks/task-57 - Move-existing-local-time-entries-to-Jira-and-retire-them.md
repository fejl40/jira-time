---
id: TASK-57
title: Move existing local time entries to Jira and retire them
status: To Do
assignee: []
created_date: '2026-10-27 14:02'
labels:
  - jira-source-of-truth
dependencies:
  - TASK-55
priority: high
ordinal: 57000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Existing users have time entries in data/documents/time-entries. On first sign-in after the change, entries Jira doesn't have yet (Not in Jira, pending deletes) are pushed or queued, the old files are copied to data/documents/backups/, and the app stops reading them. Nothing is deleted without a backup.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Entries not yet in Jira are sent or put in the retry queue; none are lost
- [ ] #2 The old time-entries folder is backed up before the app stops using it
- [ ] #3 Runs once per user and is safe to run again
- [ ] #4 Tests cover a mix of synced, unsynced and pending-delete entries
<!-- AC:END -->
