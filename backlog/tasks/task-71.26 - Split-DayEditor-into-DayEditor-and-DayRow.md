---
id: TASK-71.26
title: Split DayEditor into DayEditor and DayRow
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 101000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
DayEditor.razor is 306 lines with row-state rules in markup. Move row state into DayWork (tested) and the row markup into Sheet/DayRow.razor. Move DayEditor and IssuePicker to Components/Sheet. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 DayEditor.razor 200 lines or fewer, DayRow.razor 150 or fewer
- [ ] #2 Row states and sync-error rule live in DayWork and are tested
- [ ] #3 Markup and data-* attributes unchanged; Shared/ holds only Count, ThemeToggle, UserRow; AGENTS.md layout updated
<!-- AC:END -->
