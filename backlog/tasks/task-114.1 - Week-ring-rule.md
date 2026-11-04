---
id: TASK-114.1
title: Week ring rule
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:15'
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
- [x] #1 Shares per week are tested, including part weeks at period edges and days off
- [x] #2 Unknown when Jira can't be read
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
WeekRing.For(PeriodSummary) in Domain/WeekRings.cs: one ring per week of the period that needs hours (ISO week number, logged, needed, share capped at 1, null share when Jira couldn't be read). Tested in WeekRingsTests: part weeks, a weekend-only edge week, days off, a week off, unknown hours.
<!-- SECTION:FINAL_SUMMARY:END -->
