---
id: TASK-71.20
title: 'Shrink Timesheet.razor: keyboard component and confetti in the header'
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:08'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 95000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Timesheet.razor is 375 lines. Its keyboard bridge and confetti tracking have nothing to do with wiring the sheet. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Timesheet.razor is 250 lines or fewer, with no JSInvokable, DotNetObjectReference or Milestones
- [x] #2 Keyboard shortcuts, focus and confetti behave as before
- [x] #3 Build and tests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Timesheet.razor is 250 lines: keyboard shortcuts and focus live in Sheet/DayKeys.razor, confetti tracking in PeriodHeader, the selected day section with time off in Sheet/DayPanel.razor, and the save animation state in SaveMotion; checked keys, focus, time off and confetti in the browser.
<!-- SECTION:FINAL_SUMMARY:END -->
