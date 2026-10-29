---
id: TASK-71.28
title: Add Format helpers for day texts
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 07:59'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 103000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Components format days with CultureInfo themselves in several formats. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Format.Day, ShortDay, LongDay (and similar) replace the inline formats
- [x] #2 No CultureInfo left in Sheet razor files
- [x] #3 FormatTests cover the helpers
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Format.Day, ShortDay, LongDay, WeekdayAndNumber, TwoLetterWeekday, Month and Percent replace the inline CultureInfo formats; no CultureInfo is left in razor files, and FormatTests cover the helpers.
<!-- SECTION:FINAL_SUMMARY:END -->
