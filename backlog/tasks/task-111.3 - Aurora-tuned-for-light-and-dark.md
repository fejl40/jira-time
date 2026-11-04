---
id: TASK-111.3
title: Aurora tuned for light and dark
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
ordinal: 179000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Light mode uses pale, low-saturation blobs that keep text contrast; dark mode deeper glowing ones. Cards stay readable over it.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Text contrast on cards stays at WCAG AA in both themes
- [x] #2 The theme toggle switches the aurora with the rest of the page
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Pale blobs (36% opacity) over slate-100 in light mode, glowing ones (30%) over slate-950 in dark; the page colour moved to <html> only so the layer shows through, and the .dark class switches it with the theme toggle. Cards stay solid here, so their contrast is unchanged.
<!-- SECTION:FINAL_SUMMARY:END -->
