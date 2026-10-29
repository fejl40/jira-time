---
id: TASK-71.17
title: Let DayWork own its suggestion rules
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
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
- [ ] #1 PeriodSheet no longer touches .Suggested, .Removed or .Drafts.Add
- [ ] #2 HasChanges and HasOwnChanges share a helper
- [ ] #3 A test covers DayWork.HasOnlySuggestions
<!-- AC:END -->
