---
id: TASK-18
title: Move existing data to its owner without losing any
status: Done
assignee: []
created_date: '2026-10-19 13:34'
updated_date: '2026-10-19 13:40'
labels: []
dependencies:
  - TASK-17
priority: high
type: feature
ordinal: 18000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Data saved before multi-user support has no owner. When the user it belongs to signs in (the Jira user id in the old profile setting), all of it is given to that user on that Jira host. A backup copy of the old data is stored first. Nothing is deleted.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Existing entries and days off are given to the user whose id matches the old profile, on the host they sign in to
- [x] #2 A backup of the old data is stored before anything is changed
- [x] #3 Another user signing in does not get the old data
- [x] #4 Running it again changes nothing
- [x] #5 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
LegacyDataMigration runs on each successful sign-in. If entries or days off without an owner exist and the old settings profile id matches the signed-in Jira user (or no profile was recorded), it first stores a LegacyBackup document (backups collection) with untouched copies of the old entries, days off and settings, then gives the entries that owner (worklog links and sync state kept) and moves days off to owner-scoped ids. Another user gets nothing; running again does nothing; old start/end entries keep their hours. Serialized with a lock. LegacyDataMigrationTests cover all of this, including a restart and the full sign-in path; checked in the browser with old-format files: the second user did not get them, Carl did, and the backup file was written.
<!-- SECTION:FINAL_SUMMARY:END -->
