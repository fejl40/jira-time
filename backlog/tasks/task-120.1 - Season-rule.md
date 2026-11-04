---
id: TASK-120.1
title: Season rule
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:22'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-120
priority: medium
ordinal: 207000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A tested rule says what season touch today has: snow in December, lights on a Danish holiday (WorkCalendar already knows them), nothing otherwise.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Tested for December days, holidays and ordinary days, on the user's clock
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Domain/SeasonTouches.cs: snow in December, lights on a Danish public holiday (DanishHolidays), both on Christmas Day and Boxing Day, nothing otherwise; Today(clock, zone) reads the day on the user's clock. Tested in SeasonTouchesTests.
<!-- SECTION:FINAL_SUMMARY:END -->
