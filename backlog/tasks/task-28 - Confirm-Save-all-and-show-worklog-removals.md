---
id: TASK-28
title: Confirm Save all and show worklog removals
status: To Do
assignee: []
created_date: '2026-10-22T19:36:00Z'
labels: []
dependencies: []
priority: medium
ordinal: 28000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Save all writes many worklogs at once, and removing a saved row deletes its worklog in Jira. Make both visible before they happen.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Save all asks for confirmation with the hours and number of days
- [ ] #2 A removed saved row stays visible, struck through, with 'Deletes the worklog in Jira' and Undo, until Save day
<!-- AC:END -->
