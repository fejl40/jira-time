---
id: TASK-71.27
title: Name repeated styles in app.css
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:22'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 102000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Long class strings for the amber notice, panel surface, issue dot, status dots and light/dark text colours are copied across components. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Each repeated string lives once in app.css as a named class or utility
- [x] #2 The pages look the same in light and dark mode
- [x] #3 Build passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
app.css has .alert-warning, .panel, .issue-dot, .dot-unsaved and .dot-not-in-jira plus text-muted, text-warning and text-unsaved utilities, replacing the copied class strings; light and dark screenshots of the timesheet, export, settings, error and phone list are pixel-identical before and after.
<!-- SECTION:FINAL_SUMMARY:END -->
