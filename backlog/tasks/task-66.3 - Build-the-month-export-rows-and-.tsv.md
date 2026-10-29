---
id: TASK-66.3
title: Build the month export rows and .tsv
status: Done
assignee: []
created_date: '2026-10-29 07:36'
updated_date: '2026-10-29 07:39'
labels:
  - feature
dependencies: []
parent_task_id: TASK-66
priority: high
ordinal: 69000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Pure domain logic: the header, one row per day of the month, one row per day and issue with hours added up, decimal comma, epic link and per-epic values, key as Beskrivelse, tab-separated text.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Every day of the month gets a row; days without time have only the date
- [x] #2 One row per day and issue with hours summed and a decimal comma
- [x] #3 Epic link and per-epic values filled when the issue has an epic, empty otherwise
- [x] #4 Tab-separated text with the exact header; tabs and newlines in values become spaces
- [x] #5 Tests cover the rows and the text
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
MonthExport in Domain: every day of the month, one row per day and issue with summed hours, decimal comma, epic link and per-epic values, key as Beskrivelse, CRLF tab-separated text with the exact header. MonthExportTests.
<!-- SECTION:FINAL_SUMMARY:END -->
