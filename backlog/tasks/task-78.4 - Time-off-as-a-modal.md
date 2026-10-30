---
id: TASK-78.4
title: Time off as a modal
status: Done
assignee: []
created_date: '2026-10-30 09:11'
updated_date: '2026-10-30 09:29'
labels: []
dependencies:
  - TASK-78.2
parent_task_id: TASK-78
ordinal: 127000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The Time off… form in the day header opens as a modal (reason, whole day or hours) and returns the TimeOff asked for; removing time off stays as it works today.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Time off… opens a modal with reason and whole day or hours
- [x] #2 Saving the modal marks the day off as before, cancelling changes nothing
- [x] #3 The modal shows busy while the request to Jira runs
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Time off… in the day header opens TimeOffModal (Components/Sheet): reason (with its icon), whole day or part of the day with hours (starting at half the day; typing in the field picks part of the day). Mark off runs the marking and the period reload inside the modal with ModalContext.WhileBusyAsync, so it shows busy (Waiting spinner, controls off, no Esc or backdrop) until Jira has been read, then returns the TimeOff. Cancel changes nothing; Undo of time off works as before. The Time off… button waits while another request to Jira runs.
<!-- SECTION:FINAL_SUMMARY:END -->
