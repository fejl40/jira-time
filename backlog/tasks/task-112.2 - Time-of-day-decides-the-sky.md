---
id: TASK-112.2
title: Time of day decides the sky
status: To Do
assignee: []
created_date: '2026-11-02 18:25'
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
- [ ] #1 Phase boundaries and blends are tested with TestClock and a time zone
- [ ] #2 The sky updates while the page stays open
<!-- AC:END -->
