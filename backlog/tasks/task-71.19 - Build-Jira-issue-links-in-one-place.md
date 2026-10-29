---
id: TASK-71.19
title: Build Jira issue links in one place
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 07:56'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 94000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
MonthExport.IssueUrl is the link builder but PeriodGrid builds '/browse/' by hand. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 `/browse/` appears in exactly one place in src
- [x] #2 Tests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
JiraIssue.Url(baseUrl, key) in Domain/Jira.cs replaces MonthExport.IssueUrl and the hand-built link in PeriodGrid, so /browse/ appears once in src.
<!-- SECTION:FINAL_SUMMARY:END -->
