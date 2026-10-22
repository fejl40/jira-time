---
id: TASK-31
title: Remind about the end of the period
status: Done
assignee: []
created_date: '2026-10-22T19:36:00Z'
updated_date: '2026-10-22T20:01:00Z'
labels: []
dependencies: []
priority: low
ordinal: 31000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Warn near the end of a period, and about an unfinished previous period.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 In the last three days of the current period, a banner shows the missing hours and the end date
- [x] #2 When the previous period has missing hours or work not in Jira, a banner links to it
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
In the last three days of the current period (PeriodSummary.EndsSoon) a banner shows the end date with hours missing so far, hours for the days left and work not in Jira. When the current period is open and the previous one has missing hours or work not in Jira, a banner links to it (hideable until reload).
<!-- SECTION:FINAL_SUMMARY:END -->
