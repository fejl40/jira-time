---
id: TASK-54
title: Save days straight to Jira
status: To Do
assignee: []
created_date: '2026-10-27 14:01'
labels:
  - jira-source-of-truth
dependencies:
  - TASK-53
priority: high
ordinal: 54000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Saving a day creates, updates and deletes the day's Jira worklogs directly, then reloads the day from Jira. Nothing about the day's time is written locally on success. Moving a row to another issue moves the worklog. The 24-hour and whole-minute rules still apply before anything is sent.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Saving a day makes its Jira worklogs match the rows, then shows the day as Jira has it
- [ ] #2 Removing a row deletes its worklog; moving a row to another issue moves it
- [ ] #3 A worklog changed in Jira since the day was loaded is not overwritten silently: the user is told and the day reloads
- [ ] #4 No time entry files are written on a successful save
- [ ] #5 Tests cover create, update, move, delete and the changed-in-Jira case
<!-- AC:END -->
