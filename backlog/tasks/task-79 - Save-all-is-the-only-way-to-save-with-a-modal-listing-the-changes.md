---
id: TASK-79
title: 'Save all is the only way to save, with a modal listing the changes'
status: Done
assignee: []
created_date: '2026-10-30 09:11'
updated_date: '2026-10-30 09:39'
labels: []
dependencies:
  - TASK-78
ordinal: 128000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Remove Save day: unsaved changes on any day are saved with Save all only. The inline Save all confirmation bar is replaced by a modal that lists what will change, per day: rows added, changed hours or issue, rows removed (worklogs deleted in Jira), with totals; confirm saves, cancel goes back. Ctrl+S opens the same modal. Days that fail stay unsaved and are shown afterwards as today.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 There is no Save day button; Save all is the only way to save
- [x] #2 Save all opens a modal listing each day's changes: added, changed and removed rows, with hours
- [x] #3 Confirming saves every changed day, cancelling saves nothing
- [x] #4 Ctrl+S opens the Save all modal
- [x] #5 The inline confirmation bar is gone
- [x] #6 Tests cover the change list
- [x] #7 AGENTS.md and README describe saving with Save all
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Save day is gone: Save all in the period header is the only way to save. It opens SaveAllModal listing each unsaved day with rows added, changed (issue or hours before → after, comment changed) and removed (worklog deleted in Jira), the day's hours before and after, and totals; days that can't be saved yet (hours that can't be read, a row without issue) say why and are left out. The list is Domain/UnsavedChanges.cs (tested in UnsavedChangesTests). Save (data-modal-result) closes the modal and the page saves as before under JiraWaits SaveAll and the JiraAction 'Save all'; Cancel saves nothing. Ctrl+S opens the same modal. The inline confirmation bar is gone. The day editor shows row errors live (DayChanges.Errors) instead of after Save day. Removed the dead Save day plumbing (JiraWait.SaveDay, SavingDay, PendingDay, PeriodSheet.UnsavedRemovals). AGENTS.md and README describe saving with Save all.
<!-- SECTION:FINAL_SUMMARY:END -->
