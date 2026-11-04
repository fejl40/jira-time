---
id: TASK-113.1
title: Frosted glass surfaces
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
ordinal: 185000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Cards, header and modals get a translucent background with backdrop-filter blur, with a solid fallback where the browser lacks backdrop-filter.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Readable over Aurora, Sky and Plain in both themes
- [x] #2 Solid fallback without backdrop-filter support
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Styles/glass.css: over Aurora and Sky (data-glass on the background layer, read with :root:has) cards and panels are 86% white / 74% slate-900 with a 20 px backdrop blur, the header (.app-header) and modals likewise; inside @supports (backdrop-filter), so other browsers keep the solid surfaces, and Plain keeps them too. Measured in the browser: muted text on the glass stays at 4.55:1 or better in light mode and 5.7:1 in dark mode over every background.
<!-- SECTION:FINAL_SUMMARY:END -->
