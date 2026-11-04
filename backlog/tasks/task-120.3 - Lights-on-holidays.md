---
id: TASK-120.3
title: Lights on holidays
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
ordinal: 209000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A string of small lights along the header glows in a slow chase on Danish holidays.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Shows only on holidays
- [x] #2 Still lights with reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Components/Shared/HolidayLights.razor right under the header: a wire of dips with 36 bulbs (18 on phones) in red, amber, green and blue, a brightness wave running along them (opacity only); lit and still with reduced motion. Takes no room or clicks.
<!-- SECTION:FINAL_SUMMARY:END -->
