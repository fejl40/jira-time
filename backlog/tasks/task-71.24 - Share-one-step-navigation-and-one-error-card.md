---
id: TASK-71.24
title: Share one step navigation and one error card
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:11'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 99000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The previous/current/next buttons and the error card with Try again are copied between pages. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Shared StepNav and ErrorCard components replace the copies
- [x] #2 Rendered HTML is unchanged
- [x] #3 Build passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Shared/StepNav and Shared/ErrorCard replace the copied ‹ current › links (PeriodHeader, ExportPage) and error cards (ExportPage, MainLayout); ErrorCard has a Compact switch for the in-page card so the rendered HTML stayed identical (compared in the browser). The timesheet's amber Jira banner is a different shape and stays as it is.
<!-- SECTION:FINAL_SUMMARY:END -->
