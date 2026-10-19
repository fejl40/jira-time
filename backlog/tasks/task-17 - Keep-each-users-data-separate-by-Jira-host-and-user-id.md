---
id: TASK-17
title: Keep each user's data separate by Jira host and user id
status: Done
assignee: []
created_date: '2026-10-19 13:34'
updated_date: '2026-10-19 13:40'
labels: []
dependencies:
  - TASK-16
priority: high
type: feature
ordinal: 17000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Time entries and days off belong to one user: the Jira host plus the user id. Every read and write is limited to the signed-in user, so users never see or change each other's work.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Entries and days off are stored with their owner (host + user id)
- [x] #2 Lists, totals, recent issues, unsynced work and days off only include the signed-in user's data
- [x] #3 A user can't load, change or delete another user's entry
- [x] #4 The same user id on two different Jira hosts are two separate users
- [x] #5 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
TimeEntry and DayOff get an Owner (UserKey.Id, a hash of host + Jira user id; day-off ids are owner + date). TimeEntryService and WorkCalendarService are now scoped and take CurrentUser: every query filters by owner, saves stamp the owner, and loading, saving over, syncing or deleting another user's entry is refused. The 24 h limit counts only the user's own hours. Without a verified user the services throw. DataSeparationTests cover lists, recent issues, unsynced work, days off, forged saves/deletes and per-host identity; checked in the browser with two users.
<!-- SECTION:FINAL_SUMMARY:END -->
