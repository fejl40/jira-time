---
id: TASK-117.1
title: Light sweep across the grid
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:32'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-117
priority: medium
ordinal: 198000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A bright band sweeps left to right over the grid's day columns, column by column.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Runs once per completion (Milestones), before the confetti
- [x] #2 Off with reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
wwwroot/finish.js lights a warm band over each day column in turn (down the day list on phones), timed by Domain/PeriodFinish.cs (about 600 ms across whatever the period length, tested); PeriodHeader's Milestones still decide the moment, and the once-per-browser key (jiraMotion.firstTime) now gates sweep, glow and confetti together, with the confetti bursting as the light reaches the last day. Nothing plays with reduced motion.
<!-- SECTION:FINAL_SUMMARY:END -->
