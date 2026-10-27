---
id: TASK-34
title: >-
  Log time on issues from earlier in the period, with a note on issues that
  aren't to do
status: Done
assignee: []
created_date: '2026-10-27 08:38'
updated_date: '2026-10-27 08:46'
labels: []
dependencies: []
priority: high
type: feature
ordinal: 34000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl wants to register time retroactively. The issue picker also offers issues that were assigned to him and in To Do or In Progress at any time during the period, even if they are Done now. Any issue can be picked (also through Jira search). When a row's issue is no longer to do (Done in Jira), the row shows a note that time is being logged on an issue that isn't to do; saving still works.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 The issue picker lists issues assigned to the user that were To Do or In Progress during the period, under their own heading, with their current status
- [x] #2 If Jira rejects the status names, it falls back to issues assigned during the period that weren't resolved before it
- [x] #3 Rows on an issue that is Done in Jira show a note that the issue isn't to do; the day can still be saved
- [x] #4 Statuses of issues picked from Jira search or already logged are looked up so the note also shows for them
- [x] #5 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
The issue picker has an Earlier this period group: issues assigned to the user that were To Do or In Progress during the period (falling back to unresolved-before-period on a 400). Statuses of picked and logged issues are looked up (cached 5 minutes), and rows on Done issues show a note while the day still saves. Covered by PeriodIssuesTests and checked in the browser against the mock Jira.
<!-- SECTION:FINAL_SUMMARY:END -->
