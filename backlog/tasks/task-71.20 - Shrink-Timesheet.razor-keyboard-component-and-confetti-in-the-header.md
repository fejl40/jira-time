---
id: TASK-71.20
title: 'Shrink Timesheet.razor: keyboard component and confetti in the header'
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
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
- [ ] #1 Timesheet.razor is 250 lines or fewer, with no JSInvokable, DotNetObjectReference or Milestones
- [ ] #2 Keyboard shortcuts, focus and confetti behave as before
- [ ] #3 Build and tests pass
<!-- AC:END -->
