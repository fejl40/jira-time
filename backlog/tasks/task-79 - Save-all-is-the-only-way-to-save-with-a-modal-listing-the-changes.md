---
id: TASK-79
title: 'Save all is the only way to save, with a modal listing the changes'
status: To Do
assignee: []
created_date: '2026-10-30 09:11'
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
- [ ] #1 There is no Save day button; Save all is the only way to save
- [ ] #2 Save all opens a modal listing each day's changes: added, changed and removed rows, with hours
- [ ] #3 Confirming saves every changed day, cancelling saves nothing
- [ ] #4 Ctrl+S opens the Save all modal
- [ ] #5 The inline confirmation bar is gone
- [ ] #6 Tests cover the change list
- [ ] #7 AGENTS.md and README describe saving with Save all
<!-- AC:END -->
