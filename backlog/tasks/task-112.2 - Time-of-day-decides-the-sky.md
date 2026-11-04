---
id: TASK-112.2
title: Time of day decides the sky
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:19'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-112
priority: medium
ordinal: 182000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A tested rule maps the user's local time to a sky phase (night, dawn, day, dusk) and a blend between neighbouring phases; the sky moves on at least every few minutes without a reload.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Phase boundaries and blends are tested with TestClock and a time zone
- [x] #2 The sky updates while the page stays open
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Domain/Sky.cs: phases start at 05:00 dawn, 08:00 day, 17:00 dusk, 20:00 night (local time in the browser's zone); the last hour of each phase blends into the next. SkyTests cover boundaries, blends, the user's zone and the clock moving on (TestClock). BackdropLayer re-reads the sky every minute on a TimeProvider timer.
<!-- SECTION:FINAL_SUMMARY:END -->
