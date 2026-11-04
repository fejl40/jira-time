---
id: TASK-119.1
title: Streak rule
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:16'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-119
priority: medium
ordinal: 204000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A tested rule counts consecutive workdays back from the last workday that are complete, skipping weekends, holidays and whole days off; today counts once complete, and an incomplete today doesn't break yesterday's streak.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Streak counting is tested across weekends, holidays, days off and period edges
- [x] #2 Uses the worklogs already read (no extra Jira requests beyond the recent days read)
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
A day counts when its saved hours (in Jira or waiting for it) reach what the day needs; when the worklogs were created isn't looked at, so filling a day late still counts (the rule the subtask describes). The count looks back over the last 60 days that each period load already reads (RecentWork.Days); a streak that goes back further shows as N+ ("At least N workdays" in the tooltip).
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Domain/Streak.cs counts complete workdays back from today (weekends, holidays, whole days off skipped, partial days off need the rest, an incomplete today doesn't break it, capped at the days read). Services/WorkStreak (scoped per tab) is counted by PeriodLoader from the recent work it reads anyway: no extra Jira request. Tested in StreakTests and WorkStreakTests.
<!-- SECTION:FINAL_SUMMARY:END -->
