---
id: TASK-114.1
title: Week ring rule
status: To Do
assignee: []
created_date: '2026-11-02 18:25'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-114
priority: medium
ordinal: 188000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A tested rule gives each week of the period its share of needed hours logged (weekly hours already exist in PeriodSummary), capped at a full ring, with unknown hours when Jira can't be read.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Shares per week are tested, including part weeks at period edges and days off
- [ ] #2 Unknown when Jira can't be read
<!-- AC:END -->
