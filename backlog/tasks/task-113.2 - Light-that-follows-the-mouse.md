---
id: TASK-113.2
title: Light that follows the mouse
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:25'
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
- [x] #1 Pointer moves update at most once per animation frame
- [x] #2 No light on touch-only devices or with reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
wwwroot/glass.js writes the pointer's place as --light-x/--light-y on the card or panel under the mouse at most once per animation frame (30 moves in one frame gave one update) and marks it data-lit; a radial gradient under the content draws the light and fades in and out via a registered --light-alpha. Mouse pointers only (hover + fine pointer), never on touch or with reduced motion (both checked).
<!-- SECTION:FINAL_SUMMARY:END -->
