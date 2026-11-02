---
id: TASK-116.1
title: Rows fly into their days
status: To Do
assignee: []
created_date: '2026-11-02 18:25'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-116
priority: medium
ordinal: 195000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
As the modal closes after a successful save, a ghost of each saved day's change flies from its line in the modal to its day cell (FLIP with recorded positions).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Only days that saved fly; days that couldn't be saved don't
- [ ] #2 Ends exactly on the cell, also when the grid scrolled
<!-- AC:END -->
