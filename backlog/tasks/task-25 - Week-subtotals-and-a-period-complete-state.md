---
id: TASK-25
title: Week subtotals and a period complete state
status: Done
assignee: []
created_date: '2026-10-22T19:36:00Z'
updated_date: '2026-10-22T20:01:00Z'
labels: []
dependencies: []
priority: medium
ordinal: 25000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Show hours per week, and a clear state when the period is done.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 The grid footer shows each week's total against its expected hours
- [x] #2 When no workday up to today is short, nothing is unsaved and everything is in Jira, the header says the period is complete
- [x] #3 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
New Domain/PeriodSummary computes logged, missing up to today, still to log and per-week totals (weeks split on Mondays, cut to the period). The grid footer has a Week row spanning each week's columns with logged / expected, green when met. The status line shows '✓ Complete so far' when nothing is short, unsaved or missing in Jira. PeriodSummaryTests cover weeks, missing, complete and EndsSoon.
<!-- SECTION:FINAL_SUMMARY:END -->
