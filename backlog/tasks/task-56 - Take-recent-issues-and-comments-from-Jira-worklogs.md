---
id: TASK-56
title: Take recent issues and comments from Jira worklogs
status: Done
assignee: []
created_date: '2026-10-27 14:01'
updated_date: '2026-10-27 14:28'
labels:
  - jira-source-of-truth
dependencies:
  - TASK-53
priority: medium
ordinal: 56000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The issue picker's recent issues and the comment suggestions come from the local entries today. Take them from the user's Jira worklogs of the last 60 days instead, cached briefly.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Recent issues come from the user's worklogs in the last 60 days, newest first
- [x] #2 Comment suggestions per issue come from those worklogs
- [x] #3 Tests cover both
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Recent issues and comments come from the user's worklogs of the last 60 days (RecentWork, TimeLog.GetRecentAsync).
<!-- SECTION:FINAL_SUMMARY:END -->
