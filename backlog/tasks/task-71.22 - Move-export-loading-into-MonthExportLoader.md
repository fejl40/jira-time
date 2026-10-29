---
id: TASK-71.22
title: Move export loading into MonthExportLoader
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:10'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 97000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
ExportPage.LoadAsync loads time, epics and values and works out rules in the page, untested. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 A MonthExportLoader service returns rows, values, no-epic issues, not-in-Jira count and Jira error
- [x] #2 ExportPage @code is about 60 lines
- [x] #3 Loader tests through TimeHarness cover a Jira failure and pending entries
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Services/MonthExportLoader loads time, epics and values per epic into a LoadedExport record (rows, values, no-epic issues, not-in-Jira count, Jira error); MonthExport has Total, IssueRowCount and IssuesWithoutEpic; ExportPage @code is 55 lines; loader tests cover Jira failures and pending entries.
<!-- SECTION:FINAL_SUMMARY:END -->
