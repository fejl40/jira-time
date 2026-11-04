---
id: TASK-115.1
title: Day fill level rule
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:16'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-115
priority: medium
ordinal: 192000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A tested rule gives each day's fill (logged against needed, capped, none for days that need nothing).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Fill per day is tested, including time off and days over their hours
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Domain/DayFill.cs: DayFill.Of gives level and state (filling, full, over) of saved hours against expected; none for days that need nothing or while Jira can't be read. DayFills keeps the period's fills and counts level changes per day for the waves. Tests in DayFillTests.
<!-- SECTION:FINAL_SUMMARY:END -->
