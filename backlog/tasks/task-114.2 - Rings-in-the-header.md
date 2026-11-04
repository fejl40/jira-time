---
id: TASK-114.2
title: Rings in the header
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:15'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-114
priority: medium
ordinal: 189000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
SVG rings, one per week, with stroke-dashoffset animating to the new share; a tooltip says the week's hours against what it needs.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Rings animate to their new value after a save or sync
- [x] #2 Tooltip and aria-label say e.g. Week 44: 32 of 40 h
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Components/Sheet/WeekRings.razor in the period header: SVG rings (pathLength 100) whose stroke-dashoffset eases to the new share after a save or sync, green with a check when closed, dashed when unknown; a new period's rings draw in. Tooltip and aria-label from Format.WeekRing: 'Week 44: 32 of 40 h'. Styles in Styles/rings.css.
<!-- SECTION:FINAL_SUMMARY:END -->
