---
id: TASK-20
title: Copy the previous workday and base Fill period on it
status: Done
assignee: []
created_date: '2026-10-22T19:35:00Z'
updated_date: '2026-10-22T19:51:00Z'
labels: []
dependencies: []
priority: high
ordinal: 20000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Most days look like the day before. Offer Copy previous workday in the day editor (same issues and split, scaled to the missing hours), and let the automatic suggestion and Fill period copy the previous worked day on an empty day instead of putting everything on one sprint issue.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Copy previous workday adds the previous worked day's issues scaled to the missing hours, in quarter hours
- [x] #2 An empty short day is suggested as a copy of the previous worked day, when there is one in the last two weeks
- [x] #3 Fill period chains: each empty day copies the day before it, including suggested days
- [x] #4 A partly logged day still gets the rest on the best-guess issue
- [x] #5 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
DayFiller.Copy copies a day's issues (grouped, comments dropped) scaled to a target in quarter hours, with the rounding difference on the biggest share so it adds up exactly. Propose takes the previous worked day's rows: an empty day copies them scaled to the missing hours; a partly logged day keeps the best-guess fill. PreviousWorkedDay looks back up to 14 days; the timesheet loads the two weeks before the period and uses each day's rows as they will be saved, so Fill period chains through suggested days. The day editor has 'Copy <day>' that replaces untouched suggestions with the copy. Tests in DayFillerTests; checked in the browser.
<!-- SECTION:FINAL_SUMMARY:END -->
