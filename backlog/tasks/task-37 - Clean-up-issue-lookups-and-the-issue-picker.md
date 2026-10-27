---
id: TASK-37
title: Clean up issue lookups and the issue picker
status: Done
assignee: []
created_date: '2026-10-27 09:03'
updated_date: '2026-10-27 09:09'
labels: []
dependencies: []
ordinal: 37000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
SprintIssues now caches sprint, period and status lookups with two hand-written caches. The picker's grouping rules are buried in the component.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 One small expiring cache type is used for all Jira issue caches
- [x] #2 The service is named for what it does
- [x] #3 The picker's grouping and status rules are a tested domain class
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
ExpiringCache<T> replaces the two hand-written caches. SprintIssues is now JiraIssueLookup (GetSprintAsync, GetPeriodAsync, GetStatusesAsync) and SprintIssue is IssueWithStatus. The picker's grouping and status rules are IssueChoices in Domain, with tests.
<!-- SECTION:FINAL_SUMMARY:END -->
