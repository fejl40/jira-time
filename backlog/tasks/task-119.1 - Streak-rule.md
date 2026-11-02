---
id: TASK-119.1
title: Streak rule
status: To Do
assignee: []
created_date: '2026-11-02 18:25'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-119
priority: medium
ordinal: 204000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A tested rule counts consecutive workdays back from the last workday that are complete, skipping weekends, holidays and whole days off; today counts once complete, and an incomplete today doesn't break yesterday's streak.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Streak counting is tested across weekends, holidays, days off and period edges
- [ ] #2 Uses the worklogs already read (no extra Jira requests beyond the recent days read)
<!-- AC:END -->
