---
id: TASK-28
title: Confirm Save all and show worklog removals
status: Done
assignee: []
created_date: '2026-10-22T19:36:00Z'
updated_date: '2026-10-22T19:48:00Z'
labels: []
dependencies: []
priority: medium
ordinal: 28000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Save all writes many worklogs at once, and removing a saved row deletes its worklog in Jira. Make both visible before they happen.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Save all asks for confirmation with the hours and number of days
- [x] #2 A removed saved row stays visible, struck through, with 'Deletes the worklog in Jira' and Undo, until Save day
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Save all now opens an inline confirmation with the number of days, the hours and how many worklogs get deleted, and saves every day with unsaved changes (edits and removals too, not only new rows). Removing a saved row keeps it struck through with 'Deletes the worklog in Jira' (or 'Removed' when it never reached Jira) and Undo until Save day. Checked in the browser.
<!-- SECTION:FINAL_SUMMARY:END -->
