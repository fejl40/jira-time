---
id: TASK-111.1
title: Aurora layer behind the page
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:15'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-111
priority: medium
ordinal: 177000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A fixed layer behind all content with 3–4 blurred blobs drifting slowly on CSS animations; never catches clicks or scrolls.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 The layer sits behind every page and never takes clicks
- [x] #2 Blobs drift with transform/opacity only, slowly (tens of seconds per loop)
- [x] #3 Still blobs with reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Components/Layout/BackdropLayer.razor draws a fixed layer (z-index -1, pointer-events none) behind every page from MainLayout; four radial-gradient blobs drift on 48–67 s CSS loops (transform only) and stand still with reduced motion.
<!-- SECTION:FINAL_SUMMARY:END -->
