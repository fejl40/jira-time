---
id: TASK-11
title: 'Fill period: suggest entries for every short workday'
status: Done
assignee: []
created_date: '2026-10-19 11:45'
updated_date: '2026-10-19 12:09'
labels: []
dependencies:
  - TASK-10
priority: medium
type: feature
ordinal: 11000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A 'Fill period' button runs Fill day for every workday in the period up to and including today that has fewer than 8 h. Proposals show in the grid as drafts; Carl reviews them and accepts all at once or day by day.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Proposals are made for every short workday up to today, none for future days
- [x] #2 The grid marks draft hours differently from saved hours
- [x] #3 Accept all and accept per day both work; discard clears the drafts
- [x] #4 After accepting all, every workday so far has 8 h (or is a day off/holiday)
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
DayFiller.ProposePeriod runs Propose for every day up to today and keeps the days that need hours. The period header has Fill period (when hours are missing), then "N h suggested on X days" with Discard all and Accept all. The grid shows suggested hours as dashed blue badges per issue/day (issues that only have drafts get their own row; unguessed drafts group under "No issue guessed yet") and a Suggested footer row. Opening a day shows its drafts for editing and accepting just that day. Accepting saves through WorklogSync, so worklogs reach Jira. Unit test for ProposePeriod; checked in the browser (light and dark) against the mock Jira: after Accept all, every workday so far had 8 h and the Missing row disappeared.
<!-- SECTION:FINAL_SUMMARY:END -->
