---
id: TASK-55
title: Keep failed Jira saves in a retry queue
status: To Do
assignee: []
created_date: '2026-10-27 14:01'
labels:
  - jira-source-of-truth
dependencies:
  - TASK-54
priority: high
ordinal: 55000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
When Jira can't be reached, a save must not be lost. The failed creates, updates and deletes are kept in a small local queue per user (the only time data stored locally), shown as Not in Jira with the reason, counted in totals, and retried by Sync now and when the period opens. Once Jira has a change it leaves the queue.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A save that fails is queued with its reason and the day shows the queued work as Not in Jira
- [ ] #2 Sync now and opening the period retry the queue; done items are removed
- [ ] #3 Queued work counts in totals and missing hours, and a queued delete hides the work
- [ ] #4 The queue is stored per user and survives a restart
- [ ] #5 Tests cover queueing, retry, partial failure and ordering of changes to the same worklog
<!-- AC:END -->
