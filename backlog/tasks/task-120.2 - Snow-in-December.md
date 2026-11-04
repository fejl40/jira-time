---
id: TASK-120.2
title: Snow in December
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
ordinal: 208000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Light snowflakes drift down (CSS, a fixed small number) over the background, behind content.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Never covers text or takes clicks
- [x] #2 Off with reduced motion
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
The page colour moves from body to html (the same change as the background layer, TASK-111), so a fixed layer at z-index -1 shows between the page colour and the content.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Components/Shared/Snowfall.razor (in MainLayout): 28 CSS flakes (14 on phones) falling and swaying with transforms only, spread with the golden ratio, nearer ones bigger and faster; a fixed layer at z-index -1 that takes no clicks; hidden with reduced motion.
<!-- SECTION:FINAL_SUMMARY:END -->
