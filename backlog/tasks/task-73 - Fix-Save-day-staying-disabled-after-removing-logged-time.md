---
id: TASK-73
title: Fix Save day staying disabled after removing logged time
status: Done
assignee: []
created_date: '2026-10-29 08:54'
updated_date: '2026-10-30 07:57'
labels:
  - bug
dependencies: []
priority: high
ordinal: 113000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl (2026-10-29): after removing a row with time already in Jira, Save day can't be pressed, so the worklog can't be deleted.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 After removing a saved row, Save day is enabled and deletes the worklog in Jira
- [ ] #2 A test covers the case
- [x] #3 Checked in the browser
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Not reproducible on the current version: removing saved rows enables Save day and deletes the worklog (checked in the browser on today, a previous-period day and a day marked off). Carl confirmed it works now; most likely he was running a build from before the 29 Oct refactor. No code change, so no new test.
<!-- SECTION:FINAL_SUMMARY:END -->
