---
id: TASK-96
title: Separate calendar hours from Jira hours in the period summary
status: To Do
assignee: []
created_date: '2026-10-30 13:06'
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
- [ ] #1 PeriodSummary groups or names its values as Jira hours (unknown without Jira) and calendar hours (always known), with summaries saying so
- [ ] #2 Tests: without Jira, logged, missing and complete are unknown or zero while expected and still to log keep their values
- [ ] #3 The end-of-period reminder still shows the hours for the days left while Jira is down
- [ ] #4 What the user sees stays the same
- [ ] #5 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
