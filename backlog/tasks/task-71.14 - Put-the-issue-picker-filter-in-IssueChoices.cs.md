---
id: TASK-71.14
title: Put the issue-picker filter in IssueChoices.cs
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:07'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 89000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
IssueFilter.cs and IssueChoices.cs cover one concept. The AGENTS.md layout misses IssueFilter.cs and Milestones.cs. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Domain/IssueFilter.cs is gone; IssuePicker builds unchanged
- [x] #2 AGENTS.md layout lists every Domain file
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
IssueFilter moved into Domain/IssueChoices.cs and IssueFilter.cs is gone; the AGENTS.md layout lists every Domain file, Milestones.cs included.
<!-- SECTION:FINAL_SUMMARY:END -->
