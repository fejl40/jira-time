---
id: TASK-8
title: 'Workday settings, public holidays and days off'
status: To Do
assignee: []
created_date: '2026-10-19 11:44'
labels: []
dependencies: []
priority: high
type: feature
ordinal: 8000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Define which days need 8 hours and when the workday runs, so the timesheet can show how many hours are missing and Fill knows where the gaps are. Mon-Fri are workdays at 8 h. Danish public holidays are skipped automatically. Carl can mark a day off (vacation, sick, other). Workday window is a setting, default 08:00-16:30 with an unlogged lunch 12:00-12:30.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Settings has workday start, end and lunch start/end with the defaults above
- [ ] #2 Danish public holidays (incl. Easter-based ones) are computed per year and are not workdays
- [ ] #3 A day can be marked off and unmarked from the day view
- [ ] #4 The grid shows each workday's missing hours and the period's missing total
- [ ] #5 Unit tests cover the holiday calendar and the expected-hours calculation
<!-- AC:END -->
