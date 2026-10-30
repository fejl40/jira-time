---
id: TASK-96
title: Separate calendar hours from Jira hours in the period summary
status: Done
assignee: []
created_date: '2026-10-30 13:06'
updated_date: '2026-10-30 13:13'
labels:
  - refactor
dependencies: []
priority: low
ordinal: 161000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
PeriodSummary mixes two kinds of hours: those that depend on Jira (logged, missing, complete), which are unknown when Jira can't be read, and those that come from the user's calendar (expected, still to log), which are always known. Today the rule is implicit: StillToLog keeps its value without Jira while Missing is zeroed. Name the split in PeriodSummary so each value says which kind it is, and pin with tests that calendar hours stay known without Jira. What the user sees stays the same, including the end-of-period reminder's 'N h for the days left' while Jira is down.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 PeriodSummary groups or names its values as Jira hours (unknown without Jira) and calendar hours (always known), with summaries saying so
- [x] #2 Tests: without Jira, logged, missing and complete are unknown or zero while expected and still to log keep their values
- [x] #3 The end-of-period reminder still shows the hours for the days left while Jira is down
- [x] #4 What the user sees stays the same
- [x] #5 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
PeriodSummary now names its two kinds of hours: Jira hours (JiraHoursKnown, Logged, NotInJira, Missing, MissingOn, IsComplete), unknown or zero when Jira can't be read, and calendar hours (ExpectedOn, StillToLog, the weeks' Expected), always known. HoursKnown is renamed JiraHoursKnown; members are grouped under the two kinds with summaries saying so. Tests pin that without Jira logged is only the waiting work, missing is zero and the period isn't complete, while expected and still to log keep their values, and that the end of a period still knows the hours for the days left. No UI change.
<!-- SECTION:FINAL_SUMMARY:END -->
