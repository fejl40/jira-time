---
id: TASK-71.17
title: Let DayWork own its suggestion rules
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 07:55'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 92000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
PeriodSheet edits DayWork's Suggested and Drafts collections directly and builds rows by hand; HasChanges and HasOwnChanges repeat one condition. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 PeriodSheet no longer touches .Suggested, .Removed or .Drafts.Add
- [x] #2 HasChanges and HasOwnChanges share a helper
- [x] #3 A test covers DayWork.HasOnlySuggestions
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
DayWork now has AddSuggestions, HasOnlySuggestions(saved) and RowOn; PeriodSheet uses them and AddRow, and HasChanges/HasOwnChanges share one private helper.
<!-- SECTION:FINAL_SUMMARY:END -->
