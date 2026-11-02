---
id: TASK-113.2
title: Light that follows the mouse
status: To Do
assignee: []
created_date: '2026-11-02 18:25'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-113
priority: medium
ordinal: 186000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A small script sets the pointer position as CSS variables on hovered cards (throttled to animation frames); a radial gradient draws the light. No light on touch devices.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Pointer moves update at most once per animation frame
- [ ] #2 No light on touch-only devices or with reduced motion
<!-- AC:END -->
