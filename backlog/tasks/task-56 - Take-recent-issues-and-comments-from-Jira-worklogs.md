---
id: TASK-56
title: Take recent issues and comments from Jira worklogs
status: To Do
assignee: []
created_date: '2026-10-27 14:01'
labels:
  - jira-source-of-truth
dependencies:
  - TASK-53
priority: medium
ordinal: 56000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The issue picker's recent issues and the comment suggestions come from the local entries today. Take them from the user's Jira worklogs of the last 60 days instead, cached briefly.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Recent issues come from the user's worklogs in the last 60 days, newest first
- [ ] #2 Comment suggestions per issue come from those worklogs
- [ ] #3 Tests cover both
<!-- AC:END -->
