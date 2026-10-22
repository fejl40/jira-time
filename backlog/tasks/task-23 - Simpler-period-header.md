---
id: TASK-23
title: Simpler period header
status: Done
assignee: []
created_date: '2026-10-22T19:36:00Z'
updated_date: '2026-10-22T20:01:00Z'
labels: []
dependencies: []
priority: medium
ordinal: 23000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The period header can show about ten items. Put navigation next to the date range, one status summary, and one primary action that fits the state.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Previous, Today and Next sit next to the period title
- [x] #2 One status line sums up total, missing, unsaved and not in Jira
- [x] #3 One primary action: Save all when there is unsaved work, otherwise Fill period when days are short
- [x] #4 Sync now sits with the not in Jira count
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
‹ Today › now sit left of the period range (the range is the heading). One status line sums up logged, missing, unsaved days and not in Jira, with a Sync button (Sync now when something is pending) that also imports from Jira. The main action is Save all (n days) with Discard all when something is unsaved, otherwise Fill period when days are short. The Timesheet nav link is active on /period pages too. Checked in the browser, light, dark and narrow.
<!-- SECTION:FINAL_SUMMARY:END -->
