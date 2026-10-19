---
id: TASK-14
title: Edit and save a whole day at once
status: Done
assignee: []
created_date: '2026-10-19 12:41'
updated_date: '2026-10-19 12:50'
labels: []
dependencies: []
priority: high
type: feature
ordinal: 14000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Fill day becomes the default way to log time. Opening a short workday (up to today) shows its work with the suggestion for the missing hours already filled in (one issue for the rest of the day by default). The user can change issues, hours and comments, add more issues, remove rows, then press Save day. Save day writes all changes at once and sends them to Jira. The separate Add entry button, per-entry editor and Fill day button are removed. Fill period stays as a shortcut for filling every short day.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Selecting a short workday up to today shows its entries plus a suggested row for the missing hours, without pressing a button
- [x] #2 Rows can be added, removed and edited (issue, hours, comment) before saving
- [x] #3 Save day validates all rows first; nothing is written if a row is invalid
- [x] #4 Save day creates, updates and removes entries and their Jira worklogs to match the rows
- [x] #5 Unsaved changes are visible, and can be reset
- [x] #6 The Add entry button, per-entry editor and Fill day button are gone
- [x] #7 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
New DayEditor replaces EntryEditor, DraftList and QuickPicks. Selecting a short workday up to today puts a suggestion for the missing hours on it automatically (no Fill day button). Rows can be edited inline (issue from sprint/recent or Search Jira…, hours, comment), added with + Add issue or Fill the rest, and removed. Status per row: Suggested, New, Changed, In Jira, Not in Jira. Save day calls WorklogSync.SaveDayAsync, which validates every row and the 24 h day limit first, then deletes removed entries (with their worklogs, via TASK-15), updates changed ones and creates new ones; unchanged synced rows are not sent, and worklog ids always come from storage. Discard changes drops unsaved rows; saved or discarded days are not re-suggested until reload. Fill period now adds suggestions to every short day and Save all saves each day through SaveDayAsync, jumping to the first day that still needs an issue. Grid shows unsaved hours dashed with an Unsaved row and updates live while editing. SaveDayTests cover create, update/remove/unchanged, empty day, invalid rows, 24 h limit, Jira down and stale worklog ids; checked in the browser (light and dark) against the mock Jira.
<!-- SECTION:FINAL_SUMMARY:END -->
