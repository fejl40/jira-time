---
id: TASK-66.4
title: Add the month export page
status: Done
assignee: []
created_date: '2026-10-29 07:36'
updated_date: '2026-10-29 07:43'
labels:
  - feature
dependencies: []
parent_task_id: TASK-66
priority: high
ordinal: 70000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A page for a calendar month with previous and next month, per-epic fields, a preview of the rows, Copy to clipboard and a .tsv download. Linked from the timesheet.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Month page with previous and next month, linked from the timesheet
- [x] #2 Per-epic fields saved on change and reflected in the rows
- [x] #3 Copy puts header and rows on the clipboard; Download gives timer-yyyy-MM.tsv
- [x] #4 Shows when Jira can't be read and when some time isn't in Jira yet
- [x] #5 Checked in the browser
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
ExportPage at /export?month=yyyy-MM with previous/next month, values per epic saved on change, preview, Copy and Download (wwwroot/export.js, UTF-8 with BOM), error state with Try again, note on time not in Jira yet. Linked from the nav and the period header (Export month). Checked in the browser against the mock.
<!-- SECTION:FINAL_SUMMARY:END -->
