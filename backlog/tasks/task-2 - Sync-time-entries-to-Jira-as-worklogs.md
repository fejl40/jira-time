---
id: TASK-2
title: Sync time entries to Jira as worklogs
status: To Do
assignee: []
created_date: '2026-10-19 08:59'
labels: []
dependencies: []
ordinal: 2000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Push local time entries to Jira as worklogs, per entry or per period. Store the returned worklog id in TimeEntry.JiraWorklogId and update or delete the worklog when the entry changes. Server/DC and Cloud use different worklog APIs and comment formats.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 An entry can be sent to Jira and shows as synced
- [ ] #2 A whole period can be sent in one action
- [ ] #3 Editing or deleting a synced entry updates or removes its worklog
- [ ] #4 Covered by tests against a fake Jira
<!-- AC:END -->
