---
id: TASK-19
title: Keep unsaved work when switching days and warn before leaving
status: To Do
assignee: []
created_date: '2026-10-22T19:35:00Z'
labels: []
dependencies: []
priority: high
ordinal: 19000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Edits to saved rows are lost when another day is picked in the grid, and drafts are dropped without a warning when changing period, reloading or leaving the page. Keep every day's unsaved edits while the page is open and ask before throwing them away.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Edits to saved rows survive picking another day and coming back
- [ ] #2 Days with unsaved changes are marked in the grid
- [ ] #3 Changing period, going to Settings, reloading or closing the tab asks first when something is unsaved
- [ ] #4 Covered by tests
<!-- AC:END -->
