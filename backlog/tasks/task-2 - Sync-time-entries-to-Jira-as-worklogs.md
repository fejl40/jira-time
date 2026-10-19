---
id: TASK-2
title: Sync time entries to Jira as worklogs
status: To Do
assignee: []
created_date: '2026-10-19 08:59'
updated_date: '2026-10-19 11:44'
labels: []
dependencies: []
priority: high
ordinal: 2000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Send every time entry to Jira as a worklog the moment it is saved. Store the returned worklog id in TimeEntry.JiraWorklogId; editing an entry updates its worklog and deleting it removes the worklog. Works for tasks and stories. Server/DC (API v2, plain-text comment) and Cloud (API v3, ADF comment) use different worklog formats. If Jira is unreachable or rejects the call, the entry is still saved locally, marked as not synced, and can be retried.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Saving a new entry creates a worklog on its issue with the right start time, duration and comment
- [ ] #2 Editing a synced entry updates its worklog, including moving it to another issue
- [ ] #3 Deleting a synced entry deletes its worklog
- [ ] #4 A failed sync keeps the entry, shows it as not synced, and offers a retry
- [ ] #5 Entries show their sync state in the day view
- [ ] #6 Covered by tests against a fake Jira
<!-- AC:END -->
