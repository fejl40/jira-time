---
id: TASK-71.26
title: Split DayEditor into DayEditor and DayRow
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:19'
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
- [x] #1 DayEditor.razor 200 lines or fewer, DayRow.razor 150 or fewer
- [x] #2 Row states and sync-error rule live in DayWork and are tested
- [x] #3 Markup and data-* attributes unchanged; Shared/ holds only Count, ThemeToggle, UserRow; AGENTS.md layout updated
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
DayWork.Rows(saved) returns WorkRow records with a RowState (Removed, New, Suggested, Changed, InJira, NotInJira) and ShowsSyncError, tested per state; row markup and row edits moved to Sheet/DayRow.razor (127 lines), DayEditor is 194 lines, the picker takes one IssueChoices, and DayEditor/IssuePicker moved to Sheet/. The record is named WorkRow because the component is DayRow. Editor HTML compared identical in the browser for suggested, in Jira, changed, new, removed, bad hours and Done rows. Shared/ also holds StepNav and ErrorCard from TASK-71.24.
<!-- SECTION:FINAL_SUMMARY:END -->
